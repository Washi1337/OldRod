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

namespace OldRod.Core.Disassembly.DataFlow
{
    public class ProgramState
    {
        public ulong IP
        {
            get;
            set;
        }

        public uint Key
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