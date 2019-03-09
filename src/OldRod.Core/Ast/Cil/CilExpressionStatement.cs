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

namespace OldRod.Core.Ast.Cil
{
    public class CilExpressionStatement : CilStatement
    {
        private CilExpression _expression;

        public CilExpressionStatement(CilExpression expression)
        {
            Expression = expression;
        }
        
        public CilExpression Expression
        {
            get => _expression;
            set
            {
                if (value?.Parent != null)
                    throw new ArgumentException("Item is already added to another node.");
                if (_expression != null)
                    _expression.Parent = null;
                _expression = value;
                if (value != null)
                    value.Parent = this;
            }
        }
        
        public override void ReplaceNode(CilAstNode node, CilAstNode newNode)
        {
            AssertNodeParents(node, newNode);
            Expression = (CilExpression) newNode;
        }

        public override IEnumerable<CilAstNode> GetChildren()
        {
            return new[] {Expression};
        }

        public override void AcceptVisitor(ICilAstVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }

        public override TResult AcceptVisitor<TResult>(ICilAstVisitor<TResult> visitor)
        {
            return visitor.VisitExpressionStatement(this);
        }

        public override string ToString()
        {
            return Expression.ToString();
        }
    }
}