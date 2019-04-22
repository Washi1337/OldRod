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

namespace OldRod.Core.Ast.Cil
{
    public class CilVariableExpression : CilExpression
    {
        private CilVariable _variable;

        public CilVariableExpression(CilVariable variable)
        {
            Variable = variable;
        }
        
        public CilVariable Variable
        {
            get => _variable;
            set
            {
                _variable?.UsedBy.Remove(this);
                _variable = value;
                if (value != null)
                {
                    value.UsedBy.Add(this);
                    ExpressionType = value.Signature.VariableType;
                }
            }
        }

        public bool IsReference
        {
            get;
            set;
        }
        
        public override void ReplaceNode(CilAstNode node, CilAstNode newNode)
        {
            throw new InvalidOperationException();
        }

        public override IEnumerable<CilAstNode> GetChildren()
        {
            return Enumerable.Empty<CilAstNode>();
        }

        public override void AcceptVisitor(ICilAstVisitor visitor)
        {
            visitor.VisitVariableExpression(this);
        }

        public override TResult AcceptVisitor<TResult>(ICilAstVisitor<TResult> visitor)
        {
            return visitor.VisitVariableExpression(this);
        }

        public override string ToString()
        {
            return IsReference
                ? $"ldloca {Variable.Name}"
                : $"ldloc {Variable.Name}";
        }
    }
}