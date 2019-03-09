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
using System.Collections.Generic;
using AsmResolver;
using AsmResolver.Net;
using AsmResolver.Net.Cts;
using OldRod.Core.Disassembly.DataFlow;

namespace OldRod.Core.Architecture
{
    public static class Utils
    {
        private static readonly IDictionary<ILStackBehaviour, VMType[]> _argumentTypes =
            new Dictionary<ILStackBehaviour, VMType[]>
            {
                [ILStackBehaviour.None] = Array.Empty<VMType>(),
                [ILStackBehaviour.PopAny] = new[] {VMType.Object},
                [ILStackBehaviour.PopPtr] = new[] {VMType.Pointer},
                [ILStackBehaviour.PopByte] = new[] {VMType.Byte},
                [ILStackBehaviour.PopWord] = new[] {VMType.Word},
                [ILStackBehaviour.PopDword] = new[] {VMType.Dword},
                [ILStackBehaviour.PopQword] = new[] {VMType.Qword},
                [ILStackBehaviour.PopReal32] = new[] {VMType.Real32},
                [ILStackBehaviour.PopReal64] = new[] {VMType.Real64},
                [ILStackBehaviour.PopDword_PopDword] = new[] {VMType.Dword, VMType.Dword},
                [ILStackBehaviour.PopQword_PopQword] = new[] {VMType.Qword, VMType.Qword},
                [ILStackBehaviour.PopObject_PopObject] = new[] {VMType.Object, VMType.Object},
                [ILStackBehaviour.PopReal32_PopReal32] = new[] {VMType.Real32, VMType.Real32},
                [ILStackBehaviour.PopReal64_PopReal64] = new[] {VMType.Real64, VMType.Real64},
                [ILStackBehaviour.PopPtr_PopPtr] = new[] {VMType.Pointer, VMType.Pointer},
                [ILStackBehaviour.PopPtr_PopByte] = new[] {VMType.Pointer, VMType.Byte},
                [ILStackBehaviour.PopPtr_PopWord] = new[] {VMType.Pointer, VMType.Word},
                [ILStackBehaviour.PopPtr_PopDword] = new[] {VMType.Pointer, VMType.Dword},
                [ILStackBehaviour.PopPtr_PopQword] = new[] {VMType.Pointer, VMType.Qword},
                [ILStackBehaviour.PopPtr_PopObject] = new[] {VMType.Pointer, VMType.Object},
            };

        private static readonly IDictionary<ILStackBehaviour, VMType> _resultTypes =
            new Dictionary<ILStackBehaviour, VMType>
            {
                [ILStackBehaviour.PushPtr] = VMType.Pointer,
                [ILStackBehaviour.PushByte] = VMType.Byte,
                [ILStackBehaviour.PushWord] = VMType.Word,
                [ILStackBehaviour.PushDword] = VMType.Dword,
                [ILStackBehaviour.PushQword] = VMType.Qword,
                [ILStackBehaviour.PushReal32] = VMType.Real32,
                [ILStackBehaviour.PushReal64] = VMType.Real64,
                [ILStackBehaviour.PushObject] = VMType.Object,
            };
        
        public static uint ReadCompressedUInt(IBinaryStreamReader reader)
        {
            uint num = 0;
            var shift = 0;
            byte current;
            do
            {
                current = reader.ReadByte();
                num |= (current & 0x7fu) << shift;
                shift += 7;
            } while((current & 0x80) != 0);
            return num;
        }

        public static uint FromCodedToken(uint codedToken)
        {
            var rid = codedToken >> 3;
            switch(codedToken & 7)
            {
                case 1:
                    return rid | 0x02000000;
                case 2:
                    return rid | 0x01000000;
                case 3:
                    return rid | 0x1b000000;
                case 4:
                    return rid | 0x0a000000;
                case 5:
                    return rid | 0x06000000;
                case 6:
                    return rid | 0x04000000;
                case 7:
                    return rid | 0x2b000000;
            }
            return rid;
        }
        
        public static VMType GetArgumentType(this ILStackBehaviour popBehaviour, int argumentIndex)
        {
            return _argumentTypes[popBehaviour][argumentIndex];
        }

        public static VMType GetResultType(this ILStackBehaviour pushBehaviour)
        {
            return _resultTypes.TryGetValue(pushBehaviour, out var type) ? type : VMType.Unknown;
        }

        public static VMType ToVMType(this ITypeDescriptor type)
        {
            if (type.Namespace != "System")
                return VMType.Object;

            switch (type.Name)
            {
                case "Boolean":
                case "Byte":
                case "SByte":
                    return VMType.Byte;
                case "Int16":
                case "UInt16":
                    return VMType.Word;
                case "Int32":
                case "UInt32":
                    return VMType.Dword;
                case "Int64":
                case "UInt64":
                    return VMType.Qword;
                case "Single":
                    return VMType.Real32;
                case "Double":
                    return VMType.Real64;
                case "IntPtr":
                case "UIntPtr":
                    return VMType.Pointer;
                case "Void":
                    return VMType.Unknown;
            }

            return VMType.Object;
        }

        public static ITypeDescriptor ToMetadataType(this VMType type, MetadataImage image)
        {
            switch (type)
            {
                case VMType.Unknown:
                case VMType.Object:
                    return image.TypeSystem.Object;
                case VMType.Pointer:
                    return image.TypeSystem.IntPtr;
                case VMType.Byte:
                    return image.TypeSystem.Byte;
                case VMType.Word:
                    return image.TypeSystem.UInt16;
                case VMType.Dword:
                    return image.TypeSystem.UInt32;
                case VMType.Qword:
                    return image.TypeSystem.UInt64;
                case VMType.Real32:
                    return image.TypeSystem.Single;
                case VMType.Real64:
                    return image.TypeSystem.Double;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static void AddOrMerge(this IList<SymbolicValue> values, int index, SymbolicValue value)
        {
            if (index < values.Count)
                values[index].MergeWith(value);
            else if (index == values.Count)
                values.Add(value);
            else
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}