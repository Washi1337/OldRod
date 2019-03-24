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

using AsmResolver;

namespace OldRod.Core.Architecture
{
    public class VMExportInfo
    {
        public static VMExportInfo FromReader(IBinaryStreamReader reader)
        {
            uint offset = reader.ReadUInt32();
            uint entryKey = offset != 0 ? reader.ReadUInt32() : 0;

            return new VMExportInfo
            {
                CodeOffset = offset,
                EntryKey = entryKey,
                Signature = VMFunctionSignature.FromReader(reader)
            };
        }
        
        public uint CodeOffset
        {
            get;
            set;
        }

        public uint EntryKey
        {
            get;
            set;
        }

        public bool IsSignatureOnly => CodeOffset == 0;
        
        public VMFunctionSignature Signature
        {
            get;
            set;
        }

        public override string ToString()
        {
            return $"{nameof(CodeOffset)}: {CodeOffset:X8}, {nameof(EntryKey)}: {EntryKey:X8}, {nameof(Signature)}: {Signature}";
        }
    }
}