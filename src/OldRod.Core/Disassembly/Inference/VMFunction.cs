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
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Memory;

namespace OldRod.Core.Disassembly.Inference
{
    public class VMFunction
    {
        public VMFunction(uint entrypointAddress, uint entryKey)
        {
            EntrypointAddress = entrypointAddress;
            EntryKey = entryKey;
        }

        public uint EntrypointAddress
        {
            get;
        }

        public uint EntryKey
        {
            get;
        }

        public uint? ExitKey
        {
            get;
            set;
        }

        public IFrameLayout FrameLayout
        {
            get;
            set;
        }

        public IDictionary<long, ILInstruction> Instructions
        {
            get;
        } = new Dictionary<long, ILInstruction>();

        public ISet<long> BlockHeaders
        {
            get;
        } = new HashSet<long>();
            
        public ISet<long> UnresolvedOffsets
        {
            get;
        } = new HashSet<long>();

        public ISet<FunctionReference> References
        {
            get;
        } = new HashSet<FunctionReference>();

        public override string ToString()
        {
            return $"function_{EntrypointAddress:X4}";
        }
    }
}