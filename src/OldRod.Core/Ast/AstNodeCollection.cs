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