// Project OldRod - A KoiVM devirtualisation utility.
// Copyright (C) 2019 Washi
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL.Pattern;

namespace OldRod.Core.Ast.IL.Transform
{
    public class FlagOperationSimplifier : ChangeAwareILAstTransform
    {
        private static readonly ILInstructionPattern FLAndConstantPattern = new ILInstructionPattern(
            // AND(op0, op1)
            ILCode.__AND_DWORD, ILOperandPattern.Null,
            // op0 = PUSHR_DWORD fl
            new ILInstructionPattern(
                ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                ILVariablePattern.FL.CaptureVar("fl")),
            // op1 = PUSHI_DWORD constant
            new ILInstructionPattern(ILCode.PUSHI_DWORD, ILOperandPattern.Any).Capture("constant"));

        private static readonly ILInstructionPattern ComparePattern = new ILInstructionPattern(
            new ILOpCodePattern(
                ILCode.CMP,
                ILCode.CMP_R32, ILCode.CMP_R64,
                ILCode.CMP_DWORD, ILCode.CMP_QWORD),
            ILOperandPattern.Null,
            ILExpressionPattern.Any.CaptureExpr("left"),
            ILExpressionPattern.Any.CaptureExpr("right"));

        private static readonly ILInstructionPattern AndRegistersPattern = new ILInstructionPattern(
            ILCode.__AND_DWORD, ILOperandPattern.Null,
            new ILInstructionPattern(
                ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                ILVariablePattern.Any.CaptureVar("left")),
            new ILInstructionPattern(
                ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                ILVariablePattern.Any.CaptureVar("right")));

        private static readonly ILInstructionPattern EqualsPattern = new ILInstructionPattern(
            ILCode.__EQUALS_DWORD, ILOperandPattern.Null,
            new ILInstructionPattern(
                ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                ILVariablePattern.Any.CaptureVar("reg")),
            new ILInstructionPattern(ILCode.PUSHI_DWORD, ILOperandPattern.Any).Capture("constant"));
        
        private readonly VMConstants _constants;

        public FlagOperationSimplifier(VMConstants constants)
        {
            _constants = constants;
        }

        public override string Name => "Flag Operation Simplifier";

        public override bool VisitCompilationUnit(ILCompilationUnit unit)
        {
            bool changed = false;
            bool singleChange = true;

            while (singleChange)
            {
                singleChange = false;
                
                foreach (var variable in unit.Variables.OfType<ILFlagsVariable>())
                {
                    if (variable.UsedBy.Count == 1)
                    {
                        var use = variable.UsedBy[0];

                        var match = FLAndConstantPattern.Match(use.Parent.Parent);
                        if (match.Success)
                        {
                            var fl = (ILFlagsVariable) ((ILVariableExpression) match.Captures["fl"][0]).Variable;
                            uint constant = (uint) ((ILInstructionExpression) match.Captures["constant"][0]).Operand;
                            var flags = _constants.ToFlags((byte) constant);

                            singleChange = TryOptimizeFlagComparison(unit, use, fl, flags);
                            if (singleChange)
                            {
                                changed = true;
                                break;
                            }
                        }
                    }

                }
            }

            return changed;
        }

        private bool TryOptimizeFlagComparison(ILCompilationUnit unit, ILVariableExpression flagUsage, ILFlagsVariable fl, VMFlags flags)
        {
            // TODO: support other flag combinations.
            bool changed = false;
            
            switch (flags)
            {
                case VMFlags.ZERO:  
                    changed |= TryOptimizeCompareWithZero(unit, flagUsage, fl);
                    break;
            }

            return changed;
        }

        private bool TryOptimizeCompareWithZero(ILCompilationUnit unit, ILVariableExpression flagUsage, ILFlagsVariable fl)
        {
            if (fl.ImplicitAssignments.Count != 1)
                return false;

            var expression = fl.ImplicitAssignments.First();
            
            MatchResult match;
            if ((match = ComparePattern.Match(expression)).Success) // (cmp(a, b); and(FL, ZERO)) <=> (a == b)
                return TryOptimizeToEquals(unit, expression, flagUsage, fl, match);
            if ((match = AndRegistersPattern.Match(expression)).Success)
                return TryOptimizeToGreaterThan(unit, expression, flagUsage, fl, match);

            return false;
        }

        private bool TryOptimizeToEquals(ILCompilationUnit unit, ILExpression implicitAssignment, ILVariableExpression flagUsage, ILFlagsVariable fl,
            MatchResult match)
        {
            // Replace FL operation with the new comparison operation.
            ILOpCode opcode;
            switch (((ILInstructionExpression) implicitAssignment).OpCode.Code)
            {
                case ILCode.CMP:
                    opcode = ILOpCodes.__EQUALS_OBJECT;
                    break;
                case ILCode.CMP_R32:
                    opcode = ILOpCodes.__EQUALS_R32;
                    break;
                case ILCode.CMP_R64:
                    opcode = ILOpCodes.__EQUALS_R64;
                    break;
                case ILCode.CMP_DWORD:
                    opcode = ILOpCodes.__EQUALS_DWORD;
                    break;
                case ILCode.CMP_QWORD:
                    opcode = ILOpCodes.__EQUALS_QWORD;
                    break;
                default:
                    return false;
            }

            // Introduce new variable for storing the result of the comparison.
            var resultVar = unit.GetOrCreateVariable("simplified_" + fl.Name);
            resultVar.VariableType = VMType.Dword;
            
            var assignment = new ILAssignmentStatement(resultVar,
                new ILInstructionExpression(-1, opcode, null, VMType.Dword)
                {
                    Arguments =
                    {
                        (ILExpression) match.Captures["left"][0].Remove(),
                        (ILExpression) match.Captures["right"][0].Remove()
                    }
                });

            fl.ImplicitAssignments.Remove(implicitAssignment);
            implicitAssignment.Parent.ReplaceWith(assignment);

            // Replace FL reference with new variable.
            flagUsage.Variable = null;
            flagUsage.Parent.Parent.ReplaceWith(new ILVariableExpression(resultVar));

            return true;
        }

        private bool TryOptimizeToGreaterThan(ILCompilationUnit unit, ILExpression implicitAssignment, ILVariableExpression flagUsage, ILFlagsVariable fl,
            MatchResult match)
        {
            // Yuck, but works.
            if (!(flagUsage.Parent.Parent.Parent is ILInstructionExpression parent)
                || parent.OpCode.Code != ILCode.NOR_DWORD
                || !(parent.Parent is ILInstructionExpression root)
                || root.OpCode.Code != ILCode.__NOT_DWORD)
                return false;

            // __EQUALS__DWORD(var0, overflow | sign) 
            var equalsMatch = EqualsPattern.Match(parent.Arguments[0]);
            if (!equalsMatch.Success)
                return false;
            
            var constant = (ILInstructionExpression) equalsMatch.Captures["constant"][0];
            if (Convert.ToByte(constant.Operand) != _constants.GetFlagMask(VMFlags.OVERFLOW | VMFlags.SIGN))
                return false;
            
            // Check if __AND_DWORD(var0, var0)
            var (left, right) = GetOperands(match);
            if (left.Variable != right.Variable
                || left.Variable.AssignedBy.Count != 1)
                return false;

            // Check if __AND_DWORD(fl2, overflow | sign | zero)
            var assignment = left.Variable.AssignedBy[0];
            var flMatch = FLAndConstantPattern.Match(assignment.Value);
            if (!flMatch.Success)
                return false;

            constant = (ILInstructionExpression) flMatch.Captures["constant"][0];
            if (Convert.ToByte(constant.Operand) != _constants.GetFlagMask(VMFlags.OVERFLOW | VMFlags.SIGN | VMFlags.ZERO))
                return false;

            // Check if CMP_xxxx(op0, op1)
            var flReference = (ILFlagsVariable) ((ILVariableExpression) flMatch.Captures["fl"][0]).Variable;
            if (flReference.ImplicitAssignments.Count != 1)
                return false;

            var flAssignment = flReference.ImplicitAssignments.First();
            var cmpMatch = ComparePattern.Match(flAssignment);
            if (!cmpMatch.Success)
                return false;
            
            // We have a match! Decide which opcode to use based on the original comparison that was made.
            ILOpCode opcode;
            switch (((ILInstructionExpression) flAssignment).OpCode.Code)
            {
                case ILCode.CMP_R32:
                    opcode = ILOpCodes.__GT_R64;
                    break;
                case ILCode.CMP_R64:
                    opcode = ILOpCodes.__GT_R64;
                    break;
                case ILCode.CMP_DWORD:
                    opcode = ILOpCodes.__GT_DWORD;
                    break;
                case ILCode.CMP_QWORD:
                    opcode = ILOpCodes.__GT_QWORD;
                    break;
                default:
                    return false;
            }

            // Introduce new variable for storing the result of the comparison.
            var resultVar = unit.GetOrCreateVariable("simplified_" + fl.Name);
            resultVar.VariableType = VMType.Dword;

            var newAssignment = new ILAssignmentStatement(resultVar,
                new ILInstructionExpression(-1, opcode, null, VMType.Dword)
                {
                    Arguments =
                    {
                        (ILExpression) cmpMatch.Captures["left"][0].Remove(),
                        (ILExpression) cmpMatch.Captures["right"][0].Remove()
                    }
                });

            // Replace assignment of FL
            fl.ImplicitAssignments.Remove(implicitAssignment);
            implicitAssignment.Parent.ReplaceWith(newAssignment);
            
            // Replace use of FL with new variable.
            root.ReplaceWith(new ILVariableExpression(resultVar));

            // Remove var0 assignment.
            assignment.Variable = null;
            assignment.Remove();
            
            // Remove comparison.
            flAssignment.Parent.Remove();

            // Clear references to variables.
            var referencesToRemove = new List<ILVariableExpression>();
            referencesToRemove.AddRange(implicitAssignment.AcceptVisitor(VariableUsageCollector.Instance));
            referencesToRemove.AddRange(assignment.AcceptVisitor(VariableUsageCollector.Instance));
            referencesToRemove.AddRange(flAssignment.AcceptVisitor(VariableUsageCollector.Instance));

            foreach (var reference in referencesToRemove)
            {
                reference.Variable = null;
                reference.Remove();
            }

            return true;
        }
        
        private static (ILVariableExpression left, ILVariableExpression right) GetOperands(MatchResult matchResult)
        {
            var left = (ILVariableExpression) matchResult.Captures["left"][0];
            var right = (ILVariableExpression) matchResult.Captures["right"][0];
            return (left, right);
        }
    }
}