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

namespace OldRod.Core.Ast.IL
{
    public class ILExpressionStatement : ILStatement
    {
        private ILExpression _expression;

        public ILExpressionStatement(ILExpression expression)
        {
            Expression = expression;
        }

        public ILExpression Expression
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

        public override string ToString()
        {
            return Expression.ToString();
        }

        public override void ReplaceNode(ILAstNode node, ILAstNode newNode)
        {
            AssertNodeParents(node, newNode);
            Expression = (ILExpression) newNode;
        }

        public override IEnumerable<ILAstNode> GetChildren()
        {
            return new[] {Expression};
        }

        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }
        
        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitExpressionStatement(this);
        }
    }
}