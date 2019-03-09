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

namespace OldRod.Core.Ast.IL
{
    public class ILAssignmentStatement : ILStatement
    {
        private ILExpression _value;
        private ILVariable _variable;

        public ILAssignmentStatement(ILVariable variable, ILExpression value)
        {
            Variable = variable;
            Value = value;
        }

        public ILVariable Variable
        {
            get => _variable;
            set
            {
                _variable?.AssignedBy.Remove(this);
                _variable = value;
                value?.AssignedBy.Add(this);
            }
        }

        public ILExpression Value
        {
            get => _value;
            set
            {
                if (value?.Parent != null)
                    throw new ArgumentException("Item is already added to another node.");
                if (_value != null)
                    _value.Parent = null;
                _value = value;
                if (value != null)
                    value.Parent = this;
            }
        }

        public override void ReplaceNode(ILAstNode node, ILAstNode newNode)
        {
            AssertNodeParents(node, newNode);
            Value = (ILExpression) newNode;
        }


        public override string ToString()
        {
            return $"{Variable.Name} = {Value}";
        }

        public override IEnumerable<ILAstNode> GetChildren()
        {
            return new[] {Value};
        }

        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitAssignmentStatement(this);
        }

        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitAssignmentStatement(this);
        }
    }
}