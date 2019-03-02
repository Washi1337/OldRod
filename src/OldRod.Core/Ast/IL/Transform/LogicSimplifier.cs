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
    public class LogicSimplifier : IILAstTransform, IILAstVisitor<bool>
    {
        // ¬(p or p) <=> ¬p
        private static readonly ILExpressionPattern NotPattern =
            new ILInstructionPattern(ILCode.NOR_DWORD, ILOperandPattern.Null(),
                new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any(),
                    ILVariablePattern.Any().Capture("left")),
                new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any(),
                    ILVariablePattern.Any().Capture("right")));

        // ¬(¬p or ¬q) <=> p or q
        private static readonly ILExpressionPattern OrPattern = new ILInstructionPattern(
            ILCode.NOR_DWORD, ILOperandPattern.Null(),
            new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any(), 
                new ILInstructionPattern(ILCode.__NOT_DWORD, ILOperandPattern.Null(), ILVariablePattern.Any().Capture("left"))),
            new ILInstructionPattern(ILCode.PUSHR_DWORD, ILOperandPattern.Any(), 
                new ILInstructionPattern(ILCode.__NOT_DWORD, ILOperandPattern.Null(), ILVariablePattern.Any().Capture("right"))));
        
        public string Name => "Logic simplifier";

        public void ApplyTransformation(ILCompilationUnit unit, ILogger logger)
        {
            while (unit.AcceptVisitor(this))
            {
                // ..
            }
        }

        public bool VisitCompilationUnit(ILCompilationUnit unit)
        {
            bool changed = false;
            foreach (var node in unit.ControlFlowGraph.Nodes)
            {
                var block = (ILAstBlock) node.UserData[ILAstBlock.AstBlockProperty];
                changed |= block.AcceptVisitor(this);
            }

            return changed;
        }

        public bool VisitBlock(ILAstBlock block)
        {
            bool changed = false;
            foreach (var statement in block.Statements)
                changed |= statement.AcceptVisitor(this);
            return changed;
        }

        public bool VisitExpressionStatement(ILExpressionStatement statement)
        {
            return statement.Expression.AcceptVisitor(this);
        }

        public bool VisitAssignmentStatement(ILAssignmentStatement statement)
        {
            return statement.Value.AcceptVisitor(this);
        }

        public bool VisitInstructionExpression(ILInstructionExpression expression)
        {
            bool changed = false;
            
            MatchResult matchResult;
            if ((matchResult = NotPattern.Match(expression)).Success)
                changed = TryOptimiseToNot(expression, matchResult);
            else if ((matchResult = OrPattern.Match(expression)).Success)
                changed = TryOptimiseToOr(matchResult, expression);
            else
            {
                foreach (var argument in expression.Arguments.ToArray())
                    changed |= argument.AcceptVisitor(this);
            }

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
                newExpression.Arguments.Add((ILExpression) left.Remove());
                expression.ReplaceWith(newExpression);
                
                return true;
            }

            return false;
        }

        private static bool TryOptimiseToOr(MatchResult matchResult, ILInstructionExpression expression)
        {
            var (left, right) = GetOperands(matchResult);
                
            // Replace with OR pseudo opcode.
            var newExpression = new ILInstructionExpression(
                expression.OriginalOffset, 
                ILOpCodes.__OR_DWORD, 
                null, 
                VMType.Dword);
            newExpression.Arguments.Add((ILExpression) left.Remove());
            newExpression.Arguments.Add((ILExpression) right.Remove());
            expression.ReplaceWith(newExpression);
                
            return true;
        }

        private static (ILVariableExpression left, ILVariableExpression right) GetOperands(MatchResult matchResult)
        {
            var left = (ILVariableExpression) matchResult.Captures["left"][0];
            var right = (ILVariableExpression) matchResult.Captures["right"][0];
            return (left, right);
        }

        public bool VisitVariableExpression(ILVariableExpression expression)
        {
            return false;
        }

        public bool VisitVCallExpression(ILVCallExpression expression)
        {
            bool changed = false;
            foreach (var argument in expression.Arguments.ToArray())
                changed |= argument.AcceptVisitor(this);
            return changed;
        }

        public bool VisitPhiExpression(ILPhiExpression expression)
        {
            return false;
        }
    }
}