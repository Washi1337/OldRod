using System;
using System.Collections.ObjectModel;

namespace OldRod.Core.Ast
{
    public class ILAstNodeCollection<TNode> : Collection<TNode>
        where TNode : ILAstNode
    {
        public ILAstNodeCollection(ILAstNode owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }
        
        public ILAstNode Owner
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