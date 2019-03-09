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

namespace OldRod.Core.Ast.IL.Transform
{
    public class FlagDataSourceMarker : IILAstVisitor
    {
        private readonly ICollection<int> _offsets;

        public FlagDataSourceMarker(ICollection<int> offsets)
        {
            _offsets = offsets;
        }
        
        public void VisitCompilationUnit(ILCompilationUnit unit)
        {
            foreach (var node in unit.ControlFlowGraph.Nodes)
            {
                var block = (ILAstBlock) node.UserData[ILAstBlock.AstBlockProperty];
                block.AcceptVisitor(this);
            }
        }

        public void VisitBlock(ILAstBlock block)
        {
            foreach (var statement in block.Statements)
                statement.AcceptVisitor(this);
        }

        public void VisitExpressionStatement(ILExpressionStatement statement)
        {
            statement.Expression.AcceptVisitor(this);
        }

        public void VisitAssignmentStatement(ILAssignmentStatement statement)
        {
            statement.Value.AcceptVisitor(this);
        }

        public void VisitInstructionExpression(ILInstructionExpression expression)
        {
            foreach (var argument in expression.Arguments)
                argument.AcceptVisitor(this);
            
            if (_offsets.Contains(expression.OriginalOffset))
            {
                expression.IsFlagDataSource = true;
            }
        }

        public void VisitVariableExpression(ILVariableExpression expression)
        {
        }

        public void VisitVCallExpression(ILVCallExpression expression)
        {
            foreach (var argument in expression.Arguments)
                argument.AcceptVisitor(this);
        }

        public void VisitPhiExpression(ILPhiExpression expression)
        {
        }
    }
}