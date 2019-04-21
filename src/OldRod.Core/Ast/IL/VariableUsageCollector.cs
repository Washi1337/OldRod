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

using System.Collections.Generic;
using System.Linq;

namespace OldRod.Core.Ast.IL
{
    public class VariableUsageCollector : IILAstVisitor<IEnumerable<ILVariableExpression>>
    {
        public IEnumerable<ILVariableExpression> VisitCompilationUnit(ILCompilationUnit unit)
        {
            var result = new List<ILVariableExpression>();

            foreach (var node in unit.ControlFlowGraph.Nodes)
            {
                var block = (ILAstBlock) node.UserData[ILAstBlock.AstBlockProperty];
                result.AddRange(block.AcceptVisitor(this));
            }

            return result;
        }

        public IEnumerable<ILVariableExpression> VisitBlock(ILAstBlock block)
        {
            var result = new List<ILVariableExpression>();
            foreach (var statement in block.Statements)
                result.AddRange(statement.AcceptVisitor(this));
            return result;
        }

        public IEnumerable<ILVariableExpression> VisitExpressionStatement(ILExpressionStatement statement)
        {
            return statement.Expression.AcceptVisitor(this);
        }

        public IEnumerable<ILVariableExpression> VisitAssignmentStatement(ILAssignmentStatement statement)
        {
            return statement.Value.AcceptVisitor(this);
        }

        public IEnumerable<ILVariableExpression> VisitInstructionExpression(ILInstructionExpression expression)
        {
            var result = new List<ILVariableExpression>();
            foreach (var argument in expression.Arguments)
                result.AddRange(argument.AcceptVisitor(this));
            return result;
        }

        public IEnumerable<ILVariableExpression> VisitVariableExpression(ILVariableExpression expression)
        {
            return new[] {expression};
        }

        public IEnumerable<ILVariableExpression> VisitVCallExpression(ILVCallExpression expression)
        {
            var result = new List<ILVariableExpression>();
            foreach (var argument in expression.Arguments)
                result.AddRange(argument.AcceptVisitor(this));
            return result;
        }

        public IEnumerable<ILVariableExpression> VisitPhiExpression(ILPhiExpression expression)
        {
            return expression.Variables;
        }

        public IEnumerable<ILVariableExpression> VisitExceptionExpression(ILExceptionExpression expression)
        {
            return Enumerable.Empty<ILVariableExpression>();
        }
        
    }
}