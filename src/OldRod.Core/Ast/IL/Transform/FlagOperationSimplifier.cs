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

        private readonly VMConstants _constants;

        public FlagOperationSimplifier(VMConstants constants)
        {
            _constants = constants;
        }

        public override string Name => "Flag Operation Simplifier";

        public override bool VisitCompilationUnit(ILCompilationUnit unit)
        {
            bool changed = false;
            
            foreach (var variable in unit.Variables.OfType<ILFlagsVariable>().ToArray())
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

                        changed |= TryOptimizeFlagComparison(unit, use, fl, flags);
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
                    foreach (var expression in fl.ImplicitAssignments.ToArray())
                    {
                        // (cmp(a, b); and(FL, ZERO)) <=> (a == b)
                        var match = ComparePattern.Match(expression);
                        if (match.Success)
                        {
                            // Introduce new variable for storing the result of the comparison.
                            var resultVar = unit.GetOrCreateVariable("simplified_" + fl.Name);
                            resultVar.VariableType = VMType.Dword;

                            // Replace FL operation with the new comparison operation.
                            var assignment = new ILAssignmentStatement(resultVar,
                                new ILInstructionExpression(-1, ILOpCodes.__EQUALS, null, VMType.Dword)
                                {
                                    Arguments =
                                    {
                                        (ILExpression) match.Captures["left"][0].Remove(),
                                        (ILExpression) match.Captures["right"][0].Remove()
                                    }
                                });
                            
                            fl.ImplicitAssignments.Remove(expression);
                            expression.Parent.ReplaceWith(assignment);
                            
                            // Replace FL reference with new variable.
                            flagUsage.Variable = null;
                            flagUsage.Parent.Parent.ReplaceWith(new ILVariableExpression(resultVar));
                            
                            changed = true;
                        }
                    }

                    break;
            }

            return changed;
        }
        
    }
}