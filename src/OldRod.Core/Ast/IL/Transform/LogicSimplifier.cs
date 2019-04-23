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
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL.Pattern;

namespace OldRod.Core.Ast.IL.Transform
{
    public class LogicSimplifier : ChangeAwareILAstTransform
    {
        /*
         * TODO: Some patterns defined in here might not be sufficient as many of these operators are commutative.
         *       For example, a or b = b or a.
         *
         *       Currently the checks do not take this into account and are hardcoded to recognize just the patterns
         *       emitted by vanilla KoiVM.
         */
        
        // ¬(p or p) <=> ¬p
        private static readonly ILExpressionPattern NotPattern = new ILInstructionPattern(
            // NOR_DWORD
            ILCode.NOR_DWORD, ILOperandPattern.Null,
            // PUSHR_DWORD(left)
            new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                ILVariablePattern.Any.CaptureVar("left")
            ),
            // PUSHR_DWORD(right)
            new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                ILVariablePattern.Any.CaptureVar("right")
            )
        );

            // ¬(¬p or ¬q) <=> p and q
        private static readonly ILExpressionPattern AndPattern = new ILInstructionPattern(
            // NOR_DWORD
            ILCode.NOR_DWORD, ILOperandPattern.Null,
            // NOT_DWORD(PUSHR_DWORD(left)) 
            new ILInstructionPattern(ILCode.__NOT_DWORD, ILOperandPattern.Null,
                new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                    ILVariablePattern.Any.CaptureVar("left")
                )
            ),
            // NOT_DWORD(PUSHR_DWORD(right))
            new ILInstructionPattern(ILCode.__NOT_DWORD, ILOperandPattern.Any,
                new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                    ILVariablePattern.Any.CaptureVar("right"))
            )
        );
        
        // ¬(¬(p or q)) <=> p or q
        private static readonly ILExpressionPattern OrPattern = new ILInstructionPattern(
            // NOT_DWORD
            ILCode.__NOT_DWORD, ILOperandPattern.Null,
            // NOR_DWORD
            new ILInstructionPattern(ILCode.NOR_DWORD, ILOperandPattern.Null,
                new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                    ILVariablePattern.Any.CaptureVar("left")
                ),
                new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                    ILVariablePattern.Any.CaptureVar("right")
                )
            )
        );
        
        // ¬((p and q) or ¬(p or q)) <=> p xor q
        private static readonly ILExpressionPattern XorPattern = new ILInstructionPattern(
            // NOR_DWORD
            ILCode.NOR_DWORD, ILOperandPattern.Null,
            // AND_DWORD(PUSHR_DWORD(left), PUSHR_DWORD(right))
            new ILInstructionPattern(ILCode.__AND_DWORD, ILOperandPattern.Null,
                new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                    ILVariablePattern.Any.CaptureVar("left")
                ),
                new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                    ILVariablePattern.Any.CaptureVar("right")
                )
            ),
            // NOR_DWORD(PUSHR_DWORD(left), PUSHR_DWORD(right))
            new ILInstructionPattern(ILCode.NOR_DWORD, ILOperandPattern.Null,
                new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                    ILVariablePattern.Any.CaptureVar("left")
                ),
                new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                    ILVariablePattern.Any.CaptureVar("right")
                )
            )
        );
        
        // a + ~b + 1 <=> a - b
        private static readonly ILExpressionPattern SubPattern = new ILInstructionPattern(
            ILCode.ADD_DWORD, ILOperandPattern.Null,
            new ILInstructionPattern(ILCode.ADD_DWORD, ILOperandPattern.Any,
                new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                    ILVariablePattern.Any.CaptureVar("left")
                ),
                new ILInstructionPattern(ILCode.__NOT_DWORD, ILOperandPattern.Any,
                    new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any,
                        ILVariablePattern.Any.CaptureVar("right"))
                )
            ),
            new ILInstructionPattern(ILCode.PUSHI_DWORD, 1u)
        );
        
        public override string Name => "Logic simplifier";
        
        public override bool VisitInstructionExpression(ILInstructionExpression expression)
        {
            // Depth-first to minimize the amount of iterations.
            bool changed = base.VisitInstructionExpression(expression);
            
            MatchResult matchResult;
            if ((matchResult = NotPattern.Match(expression)).Success)
                changed = TryOptimiseToNot(expression, matchResult);
            else if ((matchResult = AndPattern.Match(expression)).Success)
                changed = TryOptimiseToAnd(matchResult, expression);
            else if ((matchResult = OrPattern.Match(expression)).Success)
                changed = TryOptimiseToOr(matchResult, expression);
            else if ((matchResult = XorPattern.Match(expression)).Success)
                changed = TryOptimiseToXor(matchResult, expression);
            else if ((matchResult = SubPattern.Match(expression)).Success)
                changed = TryOptimiseToSub(matchResult, expression);

            return changed;
        }

        private static bool TryOptimiseToNot(ILInstructionExpression expression, MatchResult matchResult)
        {
            var (left, right) = GetOperands(matchResult);
            if (left.Variable == right.Variable)
            {
                // Unregister one of the variable uses.
                right.Variable = null;
                
                // Replace with NOT pseudo opcode.
                var newExpression = new ILInstructionExpression(
                    expression.OriginalOffset, 
                    ILOpCodes.__NOT_DWORD, 
                    null, 
                    VMType.Dword);
                newExpression.Arguments.Add((ILExpression) left.Parent.Remove());
                newExpression.FlagsVariable = expression.FlagsVariable;
                expression.FlagsVariable = null;
                expression.ReplaceWith(newExpression);
                
                return true;
            }

            return false;
        }

        private static bool TryOptimiseToAnd(MatchResult matchResult, ILInstructionExpression expression)
        {
            var (left, right) = GetOperands(matchResult);
                
            // Replace with AND pseudo opcode.
            var newExpression = new ILInstructionExpression(
                expression.OriginalOffset, 
                ILOpCodes.__AND_DWORD, 
                null, 
                VMType.Dword);
            newExpression.Arguments.Add((ILExpression) left.Parent.Remove());
            newExpression.Arguments.Add((ILExpression) right.Parent.Remove());
            newExpression.FlagsVariable = expression.FlagsVariable;
            expression.FlagsVariable = null;
            expression.ReplaceWith(newExpression);
                
            return true;
        }

        private bool TryOptimiseToOr(MatchResult matchResult, ILInstructionExpression expression)
        {
            var (left, right) = GetOperands(matchResult);
                
            // Replace with OR pseudo opcode.
            var newExpression = new ILInstructionExpression(
                expression.OriginalOffset, 
                ILOpCodes.__OR_DWORD, 
                null, 
                VMType.Dword);
            newExpression.Arguments.Add((ILExpression) left.Parent.Remove());
            newExpression.Arguments.Add((ILExpression) right.Parent.Remove());
            newExpression.FlagsVariable = expression.FlagsVariable;
            expression.FlagsVariable = null;
            expression.ReplaceWith(newExpression);
                
            return true;
        }

        private bool TryOptimiseToXor(MatchResult matchResult, ILInstructionExpression expression)
        {
            var lefts = matchResult.Captures["left"];
            var rights = matchResult.Captures["right"];

            if (((ILVariableExpression) lefts[0]).Variable == ((ILVariableExpression) lefts[1]).Variable
                && ((ILVariableExpression) rights[0]).Variable == ((ILVariableExpression) rights[1]).Variable)
            {
                // Unregister remaining variable references.
                ((ILVariableExpression) lefts[1]).Variable = null;
                ((ILVariableExpression) rights[1]).Variable = null;

                // Replace with XOR pseudo opcode.  
                var newExpression = new ILInstructionExpression(
                    expression.OriginalOffset,
                    ILOpCodes.__XOR_DWORD,
                    null,
                    VMType.Dword);
                newExpression.Arguments.Add((ILExpression) lefts[0].Parent.Remove());
                newExpression.Arguments.Add((ILExpression) rights[0].Parent.Remove());
                newExpression.FlagsVariable = expression.FlagsVariable;
                expression.FlagsVariable = null;
                expression.ReplaceWith(newExpression);

                return true;
            }

            return false;
        }

        private bool TryOptimiseToSub(MatchResult matchResult, ILInstructionExpression expression)
        {
            var (left, right) = GetOperands(matchResult);
            
            // Replace with SUB pseudo opcode.
            var newExpression = new ILInstructionExpression(
                expression.OriginalOffset, 
                ILOpCodes.__SUB_DWORD, 
                null, 
                VMType.Dword);
            newExpression.Arguments.Add((ILExpression) left.Parent.Remove());
            newExpression.Arguments.Add((ILExpression) right.Parent.Remove());
            newExpression.FlagsVariable = expression.FlagsVariable;
            expression.FlagsVariable = null;
            expression.ReplaceWith(newExpression);

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