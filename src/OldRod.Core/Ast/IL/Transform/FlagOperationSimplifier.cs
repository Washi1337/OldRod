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

        private static readonly ILInstructionPattern BigRelationalPattern = new ILInstructionPattern(
            ILCode.__NOT_DWORD, ILOperandPattern.Null,
            new ILInstructionPattern(ILCode.NOR_DWORD, ILOperandPattern.Null,
                new ILInstructionPattern(ILCode.__EQUALS_DWORD, ILOperandPattern.Null,
                    new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any, 
                        ILVariablePattern.Any.CaptureVar("var")),
                    new ILInstructionPattern(ILCode.PUSHI_DWORD, ILOperandPattern.Any).Capture("constant1")),
                new ILInstructionPattern(ILCode.__AND_DWORD, ILOperandPattern.Null,
                    new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                        ILVariablePattern.FL.CaptureVar("fl")),
                    new ILInstructionPattern(ILCode.PUSHI_DWORD, ILOperandPattern.Any).Capture("constant2"))));
        
        private readonly VMConstants _constants;

        public FlagOperationSimplifier(VMConstants constants)
        {
            _constants = constants;
        }

        public override string Name => "Flag Operation Simplifier";

        public override bool VisitCompilationUnit(ILCompilationUnit unit)
        {
            bool changed = false;
            bool continueLoop = true;

            while (continueLoop)
            {
                continueLoop = false;
                
                foreach (var variable in unit.Variables.OfType<ILFlagsVariable>())
                {
                    if (variable.UsedBy.Count == 1)
                    {
                        var use = variable.UsedBy[0];

                        var andExpression = use.Parent.Parent;
                        var andMatch = FLAndConstantPattern.Match(andExpression);
                        if (andMatch.Success)
                        {
                            var fl = (ILFlagsVariable) ((ILVariableExpression) andMatch.Captures["fl"][0]).Variable;
                            var constant = (ILInstructionExpression) andMatch.Captures["constant"][0];
                            var flags = _constants.ToFlags((byte) (uint) constant.Operand);

                            continueLoop = TryOptimizeFlagComparison(unit, use, fl, flags);
                            if (continueLoop)
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
                    changed |= TryOptimizeCompareWithZero(unit, fl, flagUsage);
                    break;
            }

            return changed;
        }

        private bool TryOptimizeCompareWithZero(ILCompilationUnit unit, ILFlagsVariable fl, ILVariableExpression flUsage)
        {
            if (fl.ImplicitAssignments.Count != 1)
                return false;

            var flAssignment = fl.ImplicitAssignments.First();
            
            MatchResult match;
            if ((match = ComparePattern.Match(flAssignment)).Success) // (cmp(a, b); equals(FL, 0)) <=> (a == b)
                return TryOptimizeToEquals(unit, fl, flUsage, flAssignment, match);
            if ((match = BigRelationalPattern.Match(flAssignment)).Success)
                return TryOptimizeToLessThan(unit, fl, flUsage, flAssignment, match);
            if ((match = AndRegistersPattern.Match(flAssignment)).Success)
                return TryOptimizeToGreaterThan(unit, fl, flUsage, flAssignment, match);

            return false;
        }

        private bool TryOptimizeToEquals(
            ILCompilationUnit unit,  
            ILFlagsVariable fl, 
            ILVariableExpression flUsage,
            ILExpression flAssignment,
            MatchResult match)
        {
            // Replace FL operation with the new comparison operation.
            ILOpCode opcode;
            switch (((ILInstructionExpression) flAssignment).OpCode.Code)
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

            fl.ImplicitAssignments.Remove(flAssignment);
            flAssignment.Parent.ReplaceWith(assignment);

            // Replace FL reference with new variable.
            flUsage.Variable = null;
            flUsage.Parent.Parent.ReplaceWith(new ILVariableExpression(resultVar));

            return true;
        }

        private bool TryOptimizeToLessThan(
            ILCompilationUnit unit,
            ILFlagsVariable finalFl,
            ILVariableExpression finalFlUsage,
            ILExpression finalFlAssignment,
            MatchResult match)
        {
            var relationalMatch = BigRelationalPattern.Match(finalFlAssignment);
            if (!relationalMatch.Success || !ValidateRelationalMatch(relationalMatch, out var variable))
                return false;

            var fl = (ILVariableExpression) relationalMatch.Captures["fl"][0];
            var flAssignment = ((ILFlagsVariable) fl.Variable).ImplicitAssignments.First();
            var andMatch = AndRegistersPattern.Match(flAssignment);
            if (!andMatch.Success)
                return false;

            if (!ValidateRemainingRelationalNodes(andMatch, variable, VMFlags.OVERFLOW | VMFlags.SIGN, out var varAssignment, out var flAssignment2, out var cmpMatch))
                return false;

            // We have a match! Decide which opcode to use based on the original comparison that was made.
            ILOpCode opcode;
            switch (((ILInstructionExpression) flAssignment2).OpCode.Code)
            {
                case ILCode.CMP_R32:
                    opcode = ILOpCodes.__LT_R32;
                    break;
                case ILCode.CMP_R64:
                    opcode = ILOpCodes.__LT_R64;
                    break;
                case ILCode.CMP_DWORD:
                    opcode = ILOpCodes.__LT_DWORD;
                    break;
                case ILCode.CMP_QWORD:
                    opcode = ILOpCodes.__LT_QWORD;
                    break;
                default:
                    return false;
            }

            // Introduce new variable for storing the result of the comparison.
            var resultVar = unit.GetOrCreateVariable("simplified_" + finalFl.Name);
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

            // Remove var0 assignment.
            varAssignment.Variable = null;
            varAssignment.Remove();

            // Remove comparison.
            flAssignment2.FlagsVariable = null;
            flAssignment2.Parent.Remove();

            // Clear references to variables.
            var referencesToRemove = new List<ILVariableExpression>();
            referencesToRemove.AddRange(flAssignment.AcceptVisitor(VariableUsageCollector.Instance));
            referencesToRemove.AddRange(varAssignment.AcceptVisitor(VariableUsageCollector.Instance));
            referencesToRemove.AddRange(flAssignment2.AcceptVisitor(VariableUsageCollector.Instance));

            foreach (var reference in referencesToRemove)
            {
                reference.Variable = null;
                reference.Remove();
            }

            flAssignment.FlagsVariable = null;
            flAssignment.Parent.Remove();

            // Replace assignment and use of FL with new variable.
            finalFlAssignment.FlagsVariable = null;
            finalFlAssignment.Parent.ReplaceWith(newAssignment);
            finalFlUsage.Variable = null;
            finalFlUsage.Parent.Parent.ReplaceWith(new ILVariableExpression(resultVar));
            
            return true;
        }

        private bool TryOptimizeToGreaterThan(
            ILCompilationUnit unit,  
            ILFlagsVariable fl,
            ILVariableExpression flUsage,
            ILExpression flAssignment, 
            MatchResult match)
        {
            var root = flUsage.Parent?.Parent?.Parent?.Parent;
            var relationalMatch = BigRelationalPattern.Match(root);
            if (!relationalMatch.Success || !ValidateRelationalMatch(relationalMatch, out var variable))
                return false;

            if (!ValidateRemainingRelationalNodes(match, variable, VMFlags.OVERFLOW | VMFlags.SIGN | VMFlags.ZERO, 
                out var varAssignment, out var flAssignment2, out var cmpMatch))
                return false;

            // We have a match! Decide which opcode to use based on the original comparison that was made.
            ILOpCode opcode;
            switch (((ILInstructionExpression) flAssignment2).OpCode.Code)
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

            // Remove var0 assignment.
            varAssignment.Variable = null;
            varAssignment.Remove();

            // Remove comparison.
            flAssignment2.Parent.Remove();

            // Clear references to variables.
            var referencesToRemove = new List<ILVariableExpression>();
            referencesToRemove.AddRange(flAssignment.AcceptVisitor(VariableUsageCollector.Instance));
            referencesToRemove.AddRange(varAssignment.AcceptVisitor(VariableUsageCollector.Instance));
            referencesToRemove.AddRange(flAssignment2.AcceptVisitor(VariableUsageCollector.Instance));

            foreach (var reference in referencesToRemove)
            {
                reference.Variable = null;
                reference.Remove();
            }

            // Replace assignment and use of FL with new variable.
            flAssignment.FlagsVariable = null;
            flAssignment.Parent.ReplaceWith(newAssignment);
            root.ReplaceWith(new ILVariableExpression(resultVar));
            
            return true;
        }

        private bool ValidateRelationalMatch(MatchResult relationalMatch, out ILVariableExpression variable)
        {
            var constant1 = (ILInstructionExpression) relationalMatch.Captures["constant1"][0];
            var constant2 = (ILInstructionExpression) relationalMatch.Captures["constant2"][0];
            variable = (ILVariableExpression) relationalMatch.Captures["var"][0];

            return (byte) (uint) constant1.Operand == _constants.GetFlagMask(VMFlags.OVERFLOW | VMFlags.SIGN)
                   && (byte) (uint) constant2.Operand == _constants.GetFlagMask(VMFlags.ZERO);
        }

        private bool ValidateRemainingRelationalNodes(MatchResult match, ILVariableExpression variable, VMFlags flags,
            out ILAssignmentStatement varAssignment, out ILExpression flAssignment2, out MatchResult cmpMatch)
        {
            varAssignment = null;
            flAssignment2 = null;
            cmpMatch = null;
            
            // Check if __AND_DWORD(var, var)
            var (left, right) = GetOperands(match);
            if (left.Variable != right.Variable
                || left.Variable != variable.Variable
                || left.Variable.AssignedBy.Count != 1)
                return false;

            // Check if __AND_DWORD(fl2, overflow | sign | zero)
            varAssignment = left.Variable.AssignedBy[0];
            var flMatch = FLAndConstantPattern.Match(varAssignment.Value);
            if (!flMatch.Success)
                return false;

            var constant3 = (ILInstructionExpression) flMatch.Captures["constant"][0];
            if ((byte) (uint) constant3.Operand != _constants.GetFlagMask(flags))
                return false;

            // Check if CMP_xxxx(op0, op1)
            var flUsage2 = (ILFlagsVariable) ((ILVariableExpression) flMatch.Captures["fl"][0]).Variable;
            if (flUsage2.ImplicitAssignments.Count != 1)
                return false;

            flAssignment2 = flUsage2.ImplicitAssignments.First();
            cmpMatch = ComparePattern.Match(flAssignment2);
            if (!cmpMatch.Success)
                return false;
            
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