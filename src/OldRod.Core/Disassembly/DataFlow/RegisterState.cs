using System;
using System.Collections.Generic;
using System.Linq;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.DataFlow
{
    public class RegisterState
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
                changed |= _registers[(VMRegisters) i].MergeWith(other[(VMRegisters) i]);
            return changed;
        }

        public RegisterState Copy()
        {
            var result = new RegisterState();
            for (int i = 0; i < (int) VMRegisters.Max; i++)
                result[(VMRegisters) i].MergeWith(_registers[(VMRegisters) i]);
            return result;
        }
        
        public override string ToString()
        {
            return "{" + string.Join(", ", _registers.Select(x => $"{x.Key}: {x.Value}")) + "}";
        }
    }
}