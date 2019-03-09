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
    public abstract class CilAstNode : IAstNode
    { 
        public CilAstNode Parent
        {
            get;
            internal set;
        }

        IAstNode IAstNode.Parent
        {
            get => Parent;
            set => Parent = (CilAstNode) value;
        }

        public CilAstNode Remove()
        {
            ReplaceWith(null);
            return this;
        }

        public void ReplaceWith(CilAstNode node)
        {
            Parent.ReplaceNode(this, node);
        }

        public abstract void ReplaceNode(CilAstNode node, CilAstNode newNode);

        public abstract IEnumerable<CilAstNode> GetChildren();
        
        IEnumerable<IAstNode> IAstNode.GetChildren()
        {
            return GetChildren();
        }

        public abstract void AcceptVisitor(ICilAstVisitor visitor);

        public abstract TResult AcceptVisitor<TResult>(ICilAstVisitor<TResult> visitor);
        
        protected void AssertNodeParents(CilAstNode node, CilAstNode newNode)
        {
            if (node.Parent != this)
                throw new ArgumentException("Item is not a member of this node.");
            if (newNode?.Parent != null)
                throw new ArgumentException("Item is already a member of another node.");
        }

    }
}