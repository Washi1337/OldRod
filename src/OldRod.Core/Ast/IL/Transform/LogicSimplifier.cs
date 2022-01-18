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
        private static readonly ILExpressionPattern NotPattern = ILAstPattern
            .Instruction(ILCode.NOR_DWORD, ILCode.NOR_QWORD)
            .WithAnyOperand()
            .WithArguments(
                ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                    .WithAnyOperand()
                    .WithArguments(ILVariablePattern.Any.CaptureVar("left")),
                ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                    .WithAnyOperand()
                    .WithArguments(ILVariablePattern.Any.CaptureVar("right"))
            );

        // ¬(¬p or ¬q) <=> p and q
        private static readonly ILExpressionPattern AndPattern = ILAstPattern
            .Instruction(ILCode.NOR_DWORD, ILCode.NOR_QWORD)
            .WithArguments(
                ILAstPattern.Instruction(ILCode.__NOT_DWORD, ILCode.__NOT_QWORD)
                    .WithAnyOperand()
                    .WithArguments(
                        ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                            .WithAnyOperand()
                            .WithArguments(ILVariablePattern.Any.CaptureVar("left"))),
                ILAstPattern.Instruction(ILCode.__NOT_DWORD, ILCode.__NOT_QWORD)
                    .WithAnyOperand()
                    .WithArguments(
                        ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                            .WithAnyOperand()
                            .WithArguments(ILVariablePattern.Any.CaptureVar("right")))
            );

        // ¬(¬(p or q)) <=> p or q
        private static readonly ILExpressionPattern OrPattern = ILAstPattern
            .Instruction(ILCode.__NOT_DWORD, ILCode.__NOT_QWORD)
            .WithArguments(
                ILAstPattern.Instruction(ILCode.NOR_DWORD, ILCode.NOR_QWORD)
                    .WithArguments(
                        ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                            .WithAnyOperand()
                            .WithArguments(ILVariablePattern.Any.CaptureVar("left")),
                        ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                            .WithAnyOperand()
                            .WithArguments(ILVariablePattern.Any.CaptureVar("right"))
                    )
            );
        
        // ¬((p and q) or ¬(p or q)) <=> p xor q
        private static readonly ILExpressionPattern XorPattern = ILAstPattern
            .Instruction(ILCode.NOR_DWORD, ILCode.NOR_QWORD)
            .WithArguments(
                ILAstPattern.Instruction(ILCode.__AND_DWORD, ILCode.__AND_QWORD)
                    .WithArguments(
                        ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                            .WithAnyOperand()
                            .WithArguments(ILVariablePattern.Any.CaptureVar("left")),
                        ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                            .WithArguments(ILVariablePattern.Any.CaptureVar("right"))
                    ),
                ILAstPattern.Instruction(ILCode.NOR_DWORD, ILCode.NOR_QWORD)
                    .WithArguments(
                        ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                            .WithAnyOperand()
                            .WithArguments(ILVariablePattern.Any.CaptureVar("left")),
                        ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                            .WithAnyOperand()
                            .WithArguments(ILVariablePattern.Any.CaptureVar("right"))
                    )
            );
            

        // a + ~b + 1 <=> a - b
        private static readonly ILExpressionPattern SubPattern = ILAstPattern
            .Instruction(ILCode.ADD_DWORD, ILCode.ADD_QWORD)
            .WithArguments(
                ILAstPattern.Instruction(ILCode.ADD_DWORD, ILCode.ADD_QWORD)
                    .WithArguments(
                        ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                            .WithAnyOperand()
                            .WithArguments(ILVariablePattern.Any.CaptureVar("left")),
                        ILAstPattern.Instruction(ILCode.__NOT_DWORD, ILCode.__NOT_QWORD)
                            .WithArguments(
                                ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                                    .WithAnyOperand()
                                    .WithArguments(ILVariablePattern.Any.CaptureVar("right"))
                            )
                    ),
                ILAstPattern.Instruction(ILCode.PUSHI_DWORD, ILCode.PUSHI_QWORD)
                    .WithOperand(1u, 1ul)
            );

        // ~a + 1 <=> -a
        private static readonly ILExpressionPattern NegIntegerPattern = ILAstPattern
            .Instruction(ILCode.ADD_DWORD, ILCode.ADD_QWORD)
            .WithArguments(
                ILAstPattern.Instruction(ILCode.__NOT_DWORD, ILCode.__NOT_QWORD)
                    .WithArguments(
                        ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                            .WithAnyOperand()
                            .WithArguments(ILVariablePattern.Any.CaptureVar("value"))
                    ),
                ILAstPattern.Instruction(ILCode.PUSHI_DWORD, ILCode.PUSHI_QWORD)
                    .WithOperand(1u, 1ul)
            );

        // 0 - a <=> -a
        private static readonly ILExpressionPattern NegRealPattern = ILAstPattern
            .Instruction(ILCode.SUB_R32, ILCode.SUB_R64)
            .WithArguments(
                ILAstPattern.Instruction(ILCode.PUSHI_DWORD, ILCode.PUSHI_QWORD)
                    .WithOperand(0u, 0ul),
                ILAstPattern.Instruction(ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD)
                    .WithAnyOperand()
                    .WithArguments(ILVariablePattern.Any.CaptureVar("value"))
            );

        public override string Name => "Logic simplifier";

        public override bool VisitInstructionExpression(ILInstructionExpression expression)
        {
            // Depth-first to minimize the amount of iterations.
            bool changed = base.VisitInstructionExpression(expression);

            MatchResult matchResult;
            if ((matchResult = NotPattern.Match(expression)).Success)
                changed = TryOptimiseToNot(matchResult, expression);
            else if ((matchResult = NegIntegerPattern.Match(expression)).Success
                     || (matchResult = NegRealPattern.Match(expression)).Success)
                changed = TryOptimiseToNeg(matchResult, expression);
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

        private static bool TryOptimiseToNot(MatchResult matchResult, ILInstructionExpression expression)
        {
            var (left, right) = GetOperands(matchResult);
            if (left.Variable == right.Variable)
            {
                // Unregister one of the variable uses.
                right.Variable = null;

                // Replace with NOT pseudo opcode.
                var newExpression = new ILInstructionExpression(
                    expression.OriginalOffset,
                    expression.OpCode.Code switch
                    {
                        ILCode.NOR_DWORD => ILOpCodes.__NOT_DWORD,
                        ILCode.NOR_QWORD => ILOpCodes.__NOT_QWORD,
                        _ => throw new ArgumentOutOfRangeException(nameof(expression))
                    },
                    null,
                    expression.OpCode.StackBehaviourPush.GetResultType());
                newExpression.Arguments.Add((ILExpression) left.Parent.Remove());
                newExpression.FlagsVariable = expression.FlagsVariable;
                expression.FlagsVariable = null;
                expression.ReplaceWith(newExpression);

                return true;
            }

            return false;
        }

        private static bool TryOptimiseToNeg(MatchResult matchResult, ILInstructionExpression expression)
        {
            var value = matchResult.Captures["value"][0];

            // Replace with neg pseudo opcode.
            var newExpression = new ILInstructionExpression(
                expression.OriginalOffset,
                expression.OpCode.Code switch
                {
                    ILCode.ADD_DWORD => ILOpCodes.__NEG_DWORD,
                    ILCode.ADD_QWORD => ILOpCodes.__NEG_QWORD,
                    ILCode.SUB_R32 => ILOpCodes.__NEG_R32,
                    ILCode.SUB_R64 => ILOpCodes.__NEG_R64,
                    _ => throw new ArgumentOutOfRangeException(nameof(expression))
                },
                null,
                expression.OpCode.StackBehaviourPush.GetResultType());
            newExpression.Arguments.Add((ILExpression) value.Remove());
            newExpression.FlagsVariable = expression.FlagsVariable;
            expression.FlagsVariable = null;
            expression.ReplaceWith(newExpression);

            return true;
        }

        private static bool TryOptimiseToAnd(MatchResult matchResult, ILInstructionExpression expression)
        {
            var (left, right) = GetOperands(matchResult);

            // Replace with AND pseudo opcode.
            var newExpression = new ILInstructionExpression(
                expression.OriginalOffset,
                expression.OpCode.Code switch
                {
                    ILCode.NOR_DWORD => ILOpCodes.__AND_DWORD,
                    ILCode.NOR_QWORD => ILOpCodes.__AND_QWORD,
                    _ => throw new ArgumentOutOfRangeException(nameof(expression))
                },
                null,
                expression.OpCode.StackBehaviourPush.GetResultType());
            newExpression.Arguments.Add((ILExpression) left.Parent.Remove());
            newExpression.Arguments.Add((ILExpression) right.Parent.Remove());
            newExpression.FlagsVariable = expression.FlagsVariable;
            expression.FlagsVariable = null;
            expression.ReplaceWith(newExpression);

            return true;
        }

        private static bool TryOptimiseToOr(MatchResult matchResult, ILInstructionExpression expression)
        {
            var (left, right) = GetOperands(matchResult);

            // Replace with OR pseudo opcode.
            var newExpression = new ILInstructionExpression(
                expression.OriginalOffset,
                expression.OpCode.Code switch
                {
                    ILCode.__NOT_DWORD => ILOpCodes.__OR_DWORD,
                    ILCode.__NOT_QWORD => ILOpCodes.__OR_QWORD,
                    _ => throw new ArgumentOutOfRangeException(nameof(expression))
                },
                null,
                expression.OpCode.StackBehaviourPush.GetResultType());
            newExpression.Arguments.Add((ILExpression) left.Parent.Remove());
            newExpression.Arguments.Add((ILExpression) right.Parent.Remove());
            newExpression.FlagsVariable = expression.FlagsVariable;
            expression.FlagsVariable = null;
            expression.ReplaceWith(newExpression);

            return true;
        }

        private static bool TryOptimiseToXor(MatchResult matchResult, ILInstructionExpression expression)
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
                    expression.OpCode.Code switch
                    {
                        ILCode.NOR_DWORD => ILOpCodes.__XOR_DWORD,
                        ILCode.NOR_QWORD => ILOpCodes.__XOR_QWORD,
                        _ => throw new ArgumentOutOfRangeException(nameof(expression))
                    },
                    null,
                    expression.OpCode.StackBehaviourPush.GetResultType());
                newExpression.Arguments.Add((ILExpression) lefts[0].Parent.Remove());
                newExpression.Arguments.Add((ILExpression) rights[0].Parent.Remove());
                newExpression.FlagsVariable = expression.FlagsVariable;
                expression.FlagsVariable = null;
                expression.ReplaceWith(newExpression);

                return true;
            }

            return false;
        }

        private static bool TryOptimiseToSub(MatchResult matchResult, ILInstructionExpression expression)
        {
            var (left, right) = GetOperands(matchResult);

            // Replace with SUB pseudo opcode.
            var newExpression = new ILInstructionExpression(
                expression.OriginalOffset,
                expression.OpCode.Code switch
                {
                    ILCode.ADD_DWORD => ILOpCodes.__SUB_DWORD,
                    ILCode.ADD_QWORD => ILOpCodes.__SUB_QWORD,
                    _ => throw new ArgumentOutOfRangeException(nameof(expression))
                },
                null,
                expression.OpCode.StackBehaviourPush.GetResultType());
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