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
    public class CilAssignmentStatement : CilStatement
    {
        private CilVariable _variable;
        private CilExpression _value;

        public CilAssignmentStatement(CilVariable variable, CilExpression value)
        {
            Variable = variable;
            Value = value;
        }
        
        public CilVariable Variable
        {
            get => _variable;
            set
            {
                _variable?.AssignedBy.Remove(this);
                _variable = value;
                _variable?.AssignedBy.Add(this);
            }
        }

        public CilExpression Value
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
        
        public override void ReplaceNode(CilAstNode node, CilAstNode newNode)
        {
            AssertNodeParents(node, newNode);
            Value = (CilExpression) newNode;   
        }

        public override IEnumerable<CilAstNode> GetChildren()
        {
            return new[] {Value};
        }

        public override void AcceptVisitor(ICilAstVisitor visitor)
        {
            visitor.VisitAssignmentStatement(this);
        }

        public override TResult AcceptVisitor<TResult>(ICilAstVisitor<TResult> visitor)
        {
            return visitor.VisitAssignmentStatement(this);
        }

        public override string ToString()
        {
            return $"stloc {Variable.Name}({Value})";
        }
        
    }
}