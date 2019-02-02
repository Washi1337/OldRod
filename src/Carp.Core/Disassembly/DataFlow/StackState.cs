using System;
using System.Collections.Generic;

namespace Carp.Core.Disassembly.DataFlow
{
    public class StackState
    {
        private readonly IList<SymbolicValue> _slots = new List<SymbolicValue>();
        
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
                throw new InvalidOperationException("Stack states are not the same size.");

            bool changed = false;
            for (int i = 0; i < _slots.Count; i++) 
                changed |= _slots[i].MergeWith(other._slots[i]);
            return changed;
        }

        public override string ToString()
        {
            return "{" + string.Join(", ", _slots) + "}";
        }
    }
}