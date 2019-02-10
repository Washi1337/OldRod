using System;

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