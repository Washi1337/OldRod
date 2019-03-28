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
using System.Reflection;
using OldRod.Core.Emulation;

namespace OldRod.Core.CodeGen
{
    public static class VmHelper
    {
        private static readonly byte FL_OVERFLOW;
        private static readonly byte FL_CARRY;
        private static readonly byte FL_ZERO;
        private static readonly byte FL_SIGN;
        private static readonly byte FL_UNSIGNED;
        private static readonly byte FL_BEHAV1;
        private static readonly byte FL_BEHAV2;
        private static readonly byte FL_BEHAV3;

        static VmHelper()
        {
            // Filled in by the compiler.
        }
        
        public static void UpdateFL(uint op1, uint op2, uint flResult, uint result, ref byte fl, byte mask)
        {
            const ulong SignMask = 1U << 31;
            byte flag = 0;
            if(result == 0)
                flag |= FL_ZERO;
            if((result & SignMask) != 0)
                flag |= FL_SIGN;
            if(((op1 ^ flResult) & (op2 ^ flResult) & SignMask) != 0)
                flag |= FL_OVERFLOW;
            if(((op1 ^ ((op1 ^ op2) & (op2 ^ flResult))) & SignMask) != 0)
                flag |= FL_CARRY;
            fl = (byte) ((fl & ~mask) | (flag & mask));
        }

        public static void UpdateFL(ulong op1, ulong op2, ulong flResult, ulong result, ref byte fl, byte mask)
        {
            const ulong SignMask = 1U << 63;
            byte flag = 0;
            if(result == 0)
                flag |= FL_ZERO;
            if((result & SignMask) != 0)
                flag |= FL_SIGN;
            if(((op1 ^ flResult) & (op2 ^ flResult) & SignMask) != 0)
                flag |= FL_OVERFLOW;
            if(((op1 ^ ((op1 ^ op2) & (op2 ^ flResult))) & SignMask) != 0)
                flag |= FL_CARRY;
            fl = (byte) ((fl & ~mask) | (flag & mask));
        }
        
        public static unsafe object ConvertToVmType(object obj, Type type)
        {
            if(type.IsEnum)
            {
                var elemType = Enum.GetUnderlyingType(type);
                return ConvertToVmType(Convert.ChangeType(obj, elemType), elemType);
            }

            switch(Type.GetTypeCode(type))
            {
//                case TypeCode.Byte:
//                    return (byte) obj;
                case TypeCode.SByte:
                    return (byte) (sbyte) obj;
                case TypeCode.Boolean:
                    return (byte) ((bool) obj ? 1 : 0);

//                case TypeCode.UInt16:
//                    return (ushort) obj;
                case TypeCode.Int16:
                    return (ushort) (short) obj;
                case TypeCode.Char:
                    return (char) obj;

//                case TypeCode.UInt32:
//                    return (uint) obj;
                case TypeCode.Int32:
                    return (uint) (int) obj;

//                case TypeCode.UInt64:
//                    return (ulong) obj;
                case TypeCode.Int64:
                    return (ulong) (long) obj;

//                case TypeCode.Single:
//                    return (float) obj;
//                case TypeCode.Double:
//                    return (double) obj;

                default:
                    if (obj is Pointer)
                        return (ulong) Pointer.Unbox(obj);
                    if (obj is IntPtr)
                        return (IntPtr) obj;
                    if (obj is UIntPtr)
                        return (ulong) (UIntPtr) obj;
//                    if(type.IsValueType)
//                        throw new NotSupportedException();
                    return obj;
            }
        }
        
    }
}