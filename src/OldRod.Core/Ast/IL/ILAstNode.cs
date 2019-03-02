using System;
using System.Collections.Generic;
using System.Linq;
using Rivers;

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

        public Node GetParentNode()
        {
            var current = this;
            while (current.Parent != null) 
                current = current.Parent;

            if (current is ILAstBlock block)
                return block.CfgNode;
            throw new ArgumentException("Node is not added to a control flow graph.");
        }

        public IEnumerable<ILAstNode> GetAncestors()
        {
            var current = Parent;
            while (current != null)
            {
                yield return current;
                current = current.Parent;
            }
        }

        public ILAstNode GetCommonAncestor(ILAstNode other)
        {
            var ancestors = new HashSet<ILAstNode>(GetAncestors());
            return other.GetAncestors().FirstOrDefault(x => ancestors.Contains(x));
        }

        public abstract IEnumerable<ILAstNode> GetChildren();
        
        IEnumerable<IAstNode> IAstNode.GetChildren()
        {
            return GetChildren();
        }
        
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