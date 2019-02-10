using System;
using System.Collections.ObjectModel;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Ast
{
    public class AstNodeCollection<TNode> : Collection<TNode>
        where TNode : IAstNode
    {
        public AstNodeCollection(IAstNode owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }
        
        public IAstNode Owner
        {
            get;
        }
        
        protected override void SetItem(int index, TNode item)
        {
            if (item.Parent != null)
                throw new ArgumentException("Item is already added to another node.");
            Items[index].Parent = null;
            base.SetItem(index, item);
            item.Parent = Owner;
        }

        protected override void ClearItems()
        {
            foreach (var item in Items)
                item.Parent = null;
            base.ClearItems();
        }

        protected override void RemoveItem(int index)
        {
            var item = Items[index];
            base.RemoveItem(index);
            item.Parent = null;
        }

        protected override void InsertItem(int index, TNode item)
        {
            if (item.Parent != null)
                throw new ArgumentException("Item is already added to another node.");
            base.InsertItem(index, item);
            item.Parent = Owner;
        }
    }
}