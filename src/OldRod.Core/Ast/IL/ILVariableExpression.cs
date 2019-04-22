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
    public class ILVariableExpression : ILExpression
    {
        private ILVariable _variable;

        public ILVariableExpression(ILVariable variable) 
            : base(variable.VariableType)
        {
            Variable = variable;
            ExpressionType = variable.VariableType;
        }

        public ILVariable Variable
        {
            get => _variable;
            set
            {
                _variable?.UsedBy.Remove(this);
                _variable = value;
                value?.UsedBy.Add(this);
            }
        }
        
        public override bool HasPotentialSideEffects => false;

        public override string ToString()
        {
            return Variable.Name;
        }

        public override void ReplaceNode(ILAstNode node, ILAstNode newNode)
        {
            throw new InvalidOperationException();
        }

        public override IEnumerable<ILAstNode> GetChildren()
        {
            return Enumerable.Empty<ILAstNode>();
        }

        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitVariableExpression(this);
        }
        
        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitVariableExpression(this);
        }

    }
}