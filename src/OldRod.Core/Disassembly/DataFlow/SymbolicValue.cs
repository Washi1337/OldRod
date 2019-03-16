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
using OldRod.Core.Architecture;
using OldRod.Core.Emulation;

namespace OldRod.Core.Disassembly.DataFlow
{
    public class SymbolicValue
    {
        public SymbolicValue()
        {   
        }
        
        public SymbolicValue(ILInstruction dataSource, VMType type)
        {
            DataSources.Add(dataSource);
            Type = type;
        }

        private SymbolicValue(IEnumerable<ILInstruction> dataSources)
        {
            DataSources.UnionWith(dataSources);
        }

        public ISet<ILInstruction> DataSources
        {
            get;
        } = new HashSet<ILInstruction>();

        public bool IsUnknown => DataSources.Count == 0;

        public VMType Type
        {
            get;
            set;
        }
        
        public bool MergeWith(SymbolicValue value)
        {
            if (ReferenceEquals(this, value))
                return false;
            
            Type = value.Type;            
            int size = DataSources.Count;
            DataSources.UnionWith(value.DataSources);
            return size != DataSources.Count;
        }
        
        public SymbolicValue Copy()
        {
            return new SymbolicValue(DataSources) {Type = Type};
        }
        
        public override string ToString()
        {
            return IsUnknown
                ? "?"
                : string.Format("{0} ({1})",
                    string.Join(" | ", DataSources.Select(x => x.Offset.ToString("X4"))),
                    Type.ToString().ToLower());
        }
        
        public VMSlot InferStackValue()
        {
            var emulator = new InstructionEmulator();
            var pushValue = DataSources.First(); // TODO: might need to verify multiple data sources.
            emulator.EmulateDependentInstructions(pushValue);
            emulator.EmulateInstruction(pushValue);
            return emulator.Stack.Pop();
        }
        
    }
}