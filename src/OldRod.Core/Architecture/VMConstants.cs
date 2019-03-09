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
using AsmResolver.Net.Cts;

namespace OldRod.Core.Architecture
{
    public class VMConstants
    {
        public IDictionary<FieldDefinition, byte> ConstantFields
        {
            get;
        } = new Dictionary<FieldDefinition, byte>();
        
        public IDictionary<byte, ILCode> OpCodes
        {
            get;
        } = new Dictionary<byte, ILCode>();
        
        public IDictionary<byte, VMFlags> Flags
        {
            get;
        } = new Dictionary<byte, VMFlags>();
        
        public IDictionary<byte, VMRegisters> Registers
        {
            get;
        } = new Dictionary<byte, VMRegisters>();
        
        public IDictionary<byte, VMCalls> VMCalls
        {
            get;
        } = new Dictionary<byte, VMCalls>();

        public byte HelperInit
        {
            get;
            set;
        }

        public IDictionary<byte, VMECallOpCode> ECallOpCodes
        {
            get;
        } = new Dictionary<byte, VMECallOpCode>();

        public byte FlagInstance
        {
            get;
            set;
        }

        public byte GetFlagMask(VMFlags flags)
        {
            byte result = 0;

            foreach (var entry in Flags)
            {
                if (flags.HasFlag(entry.Value))
                    result |= entry.Key;
            }

            return result;
        }
    }
}