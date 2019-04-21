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
using System.Collections;
using System.Collections.Generic;

namespace OldRod.Core.Disassembly.DataFlow
{
    public class StackState : IEnumerable<SymbolicValue>
    {
        private readonly IList<SymbolicValue> _slots = new List<SymbolicValue>();

        public int Count => _slots.Count;

        public SymbolicValue Top => _slots.Count == 0 ? null : _slots[_slots.Count - 1];
        
        public void Push(SymbolicValue slot)
        {
            _slots.Add(slot);
        }

        public SymbolicValue Pop()
        {
            var value = _slots[_slots.Count - 1];
            _slots.RemoveAt(_slots.Count - 1);
            return value;
        }

        public StackState Copy()
        {
            var copy = new StackState();
            foreach (var value in _slots)
                copy._slots.Add(value.Copy());
            return copy;
        }

        public bool MergeWith(StackState other)
        {
            if (other._slots.Count != _slots.Count)
                throw new DisassemblyException("Stack states are not the same size.");

            bool changed = false;
            for (int i = 0; i < _slots.Count; i++) 
                changed |= _slots[i].MergeWith(other._slots[i]);
            return changed;
        }

        public IEnumerator<SymbolicValue> GetEnumerator()
        {
            return _slots.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return "{" + string.Join(", ", _slots) + "}";
        }
    }
}