using System.Collections.Generic;
using Carp.Core.Architecture;

namespace Carp.Core.Disassembly.DataFlow
{
    public class ProgramState
    {
        public ulong IP
        {
            get;
            set;
        }
        
        public StackState Stack
        {
            get;
            private set;
        } = new StackState();

        public RegisterState Registers
        {
            get;
            private set;
        } = new RegisterState();

        public bool MergeWith(ProgramState other)
        {
            return Stack.MergeWith(other.Stack) | Registers.MergeWith(other.Registers);
        }

        public ProgramState Copy()
        {
            return new ProgramState
            {
                IP = IP,
                Stack = Stack.Copy(),
                Registers = Registers.Copy()
            };
        }

        public override string ToString()
        {
            return $"{nameof(Stack)}: {Stack}, {nameof(Registers)}: {Registers}";
        }
        
    }
}