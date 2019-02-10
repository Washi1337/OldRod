using System;

namespace OldRod.Core.Ast.IL
{
    public abstract class ILAstNode : IAstNode
    {
        public ILAstNode Parent
        {
            get;
            internal set;
        }

        IAstNode IAstNode.Parent
        {
            get => Parent;
            set => Parent = (ILAstNode) value;
        }

        public ILAstNode Remove()
        {
            ReplaceWith(null);
            return this;
        }

        public void ReplaceWith(ILAstNode node)
        {
            Parent.ReplaceNode(this, node);
        }
        
        public abstract void ReplaceNode(ILAstNode node, ILAstNode newNode);
        
        public abstract void AcceptVisitor(IILAstVisitor visitor);
        
        public abstract TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor);

        protected void AssertNodeParents(ILAstNode node, ILAstNode newNode)
        {
            if (node.Parent != this)
                throw new ArgumentException("Item is not a member of this node.");
            if (newNode?.Parent != null)
                throw new ArgumentException("Item is already a member of another node.");
        }
    }
}