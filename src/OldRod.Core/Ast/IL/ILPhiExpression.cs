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
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL
{
    public class ILPhiExpression : ILExpression
    {
        public ILPhiExpression(params ILVariableExpression[] variables)
            : this(variables.AsEnumerable())
        {
        }

        public ILPhiExpression(IEnumerable<ILVariableExpression> variables)
            : base(VMType.Object)
        {
            Variables = new AstNodeCollection<ILVariableExpression>(this);
            foreach (var variable in variables)
                Variables.Add(variable);
            ExpressionType = Variables[0].ExpressionType;
        }
        
        public override bool HasPotentialSideEffects => false;
        
        public IList<ILVariableExpression> Variables
        {
            get;
        }

        public override void ReplaceNode(ILAstNode node, ILAstNode newNode)
        {
            AssertNodeParents(node, newNode);
            int index = Variables.IndexOf((ILVariableExpression) node);
            
            if (newNode == null)
                Variables.RemoveAt(index);
            else
                Variables[index] = (ILVariableExpression) newNode;
        }

        public override IEnumerable<ILAstNode> GetChildren()
        {
            return Variables;
        }

        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitPhiExpression(this);
        }

        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitPhiExpression(this);
        }

        public override string ToString()
        {
            return $"φ({string.Join(", ", Variables)})";
        }
    }
}