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
using System.Linq;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.DataFlow
{
    public class RegisterState : IEnumerable<KeyValuePair<VMRegisters, SymbolicValue>>
    {
        private readonly IDictionary<VMRegisters, SymbolicValue> _registers = new Dictionary<VMRegisters, SymbolicValue>();

        public RegisterState()
        {
            for (int i = 0; i < (int) VMRegisters.Max; i++)
                _registers[(VMRegisters) i] = new SymbolicValue();
        }

        public SymbolicValue this[VMRegisters register]
        {
            get => _registers[register];
            set => _registers[register] = value ?? throw new ArgumentNullException(nameof(value));
        }

        public bool MergeWith(RegisterState other)
        {
            bool changed = false;
            for (int i = 0; i < (int) VMRegisters.Max; i++)
            {
                var reg = (VMRegisters) i;
                if (reg != VMRegisters.IP)
                    changed |= _registers[reg].MergeWith(other[reg]);
            }

            return changed;
        }

        public RegisterState Copy()
        {
            var result = new RegisterState();
            for (int i = 0; i < (int) VMRegisters.Max; i++)
                result[(VMRegisters) i].MergeWith(_registers[(VMRegisters) i]);
            return result;
        }

        public IEnumerator<KeyValuePair<VMRegisters, SymbolicValue>> GetEnumerator()
        {
            return _registers.Where(x => !x.Value.IsUnknown).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return "{"
                   + string.Join(", ", _registers.Where(x => !x.Value.IsUnknown).Select(x => $"{x.Key}: {x.Value}"))
                   + "}";
        }
    }
}