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

using System.Collections.Generic;
using System.Linq;

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

        public Stack<EHFrame> EHStack
        {
            get;
        } = new Stack<EHFrame>();

        public bool IgnoreExitKey
        {
            get;
            set;
        }
        
        public bool MergeWith(ProgramState other)
        {
            return Stack.MergeWith(other.Stack) | Registers.MergeWith(other.Registers);
        }

        public ProgramState Copy()
        {
            var copy = new ProgramState
            {
                IP = IP,
                Key = Key,
                Stack = Stack.Copy(),
                Registers = Registers.Copy(),
                IgnoreExitKey = IgnoreExitKey
            };
           
            foreach (var value in EHStack.Reverse())
                copy.EHStack.Push(value);
            
            return copy;
        }

        public override string ToString()
        {
            return $"{nameof(Stack)}: {Stack}, {nameof(Registers)}: {Registers}";
        }
        
    }
}