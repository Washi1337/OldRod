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
using System.Text;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.DataFlow
{
    public class RegisterState
    {
        private readonly SymbolicValue[] _registers = new SymbolicValue[(int) VMRegisters.Max];

        public RegisterState()
        {
            for (int i = 0; i < (int) VMRegisters.Max; i++)
                _registers[i] = new SymbolicValue();
        }

        public SymbolicValue this[VMRegisters register]
        {
            get => _registers[(int) register];
            set => _registers[(int) register] = value ?? throw new ArgumentNullException(nameof(value));
        }

        public bool MergeWith(RegisterState other)
        {
            bool changed = false;
            for (int i = 0; i < (int) VMRegisters.Max; i++)
            {
                var reg = (VMRegisters) i;
                if (reg != VMRegisters.IP)
                    changed |= _registers[(int) reg].MergeWith(other[reg]);
            }

            return changed;
        }

        public RegisterState Copy()
        {
            var result = new RegisterState();
            for (int i = 0; i < (int) VMRegisters.Max; i++)
                result[(VMRegisters) i].MergeWith(_registers[i]);
            return result;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            
            builder.Append('{');
            for (int i = 0; i < (int) VMRegisters.Max; i++)
            {
                if (builder.Length > 1)
                    builder.Append(", ");
                if (!_registers[i].IsUnknown)
                    builder.AppendFormat("{0}: {1}", (VMRegisters) i, _registers[i]);
            }
            builder.Append('}');
            
            return builder.ToString();
        }
    }
}