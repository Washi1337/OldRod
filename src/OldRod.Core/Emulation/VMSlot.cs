using System.Runtime.InteropServices;

namespace OldRod.Core.Emulation
{
    [StructLayout(LayoutKind.Explicit)]
    public struct VMSlot
    {
        [FieldOffset(0)] private ulong u8;
        [FieldOffset(0)] private double r8;
        [FieldOffset(0)] private uint u4;
        [FieldOffset(0)] private float r4;
        [FieldOffset(0)] private ushort u2;
        [FieldOffset(0)] private byte u1;
        [FieldOffset(8)] private object o;

        public ulong U8
        {
            get { return u8; }
            set
            {
                u8 = value;
                o = null;
            }
        }

        public uint U4
        {
            get { return u4; }
            set
            {
                u4 = value;
                o = null;
            }
        }

        public ushort U2
        {
            get { return u2; }
            set
            {
                u2 = value;
                o = null;
            }
        }

        public byte U1
        {
            get { return u1; }
            set
            {
                u1 = value;
                o = null;
            }
        }

        public double R8
        {
            get { return r8; }
            set
            {
                r8 = value;
                o = null;
            }
        }

        public float R4
        {
            get { return r4; }
            set
            {
                r4 = value;
                o = null;
            }
        }

        public object O
        {
            get { return o; }
            set
            {
                o = value;
                u8 = 0;
            }
        }

        public static readonly VMSlot Null;

//        public static unsafe VMSlot FromObject(object obj, Type type)
//        {
//            if(type.IsEnum)
//            {
//                var elemType = Enum.GetUnderlyingType(type);
//                return FromObject(Convert.ChangeType(obj, elemType), elemType);
//            }
//
//            switch(Type.GetTypeCode(type))
//            {
//                case TypeCode.Byte:
//                    return new VMSlot {u1 = (byte) obj};
//                case TypeCode.SByte:
//                    return new VMSlot {u1 = (byte) (sbyte) obj};
//                case TypeCode.Boolean:
//                    return new VMSlot {u1 = (byte) ((bool) obj ? 1 : 0)};
//
//                case TypeCode.UInt16:
//                    return new VMSlot {u2 = (ushort) obj};
//                case TypeCode.Int16:
//                    return new VMSlot {u2 = (ushort) (short) obj};
//                case TypeCode.Char:
//                    return new VMSlot {u2 = (char) obj};
//
//                case TypeCode.UInt32:
//                    return new VMSlot {u4 = (uint) obj};
//                case TypeCode.Int32:
//                    return new VMSlot {u4 = (uint) (int) obj};
//
//                case TypeCode.UInt64:
//                    return new VMSlot {u8 = (ulong) obj};
//                case TypeCode.Int64:
//                    return new VMSlot {u8 = (ulong) (long) obj};
//
//                case TypeCode.Single:
//                    return new VMSlot {r4 = (float) obj};
//                case TypeCode.Double:
//                    return new VMSlot {r8 = (double) obj};
//
//                default:
//                    if(obj is Pointer)
//                        return new VMSlot {u8 = (ulong) Pointer.Unbox(obj)};
//                    if(obj is IntPtr)
//                        return new VMSlot {u8 = (ulong) (IntPtr) obj};
//                    if(obj is UIntPtr)
//                        return new VMSlot {u8 = (ulong) (UIntPtr) obj};
//                    if(type.IsValueType)
//                        return new VMSlot {o = ValueTypeBox.Box(obj, type)};
//                    return new VMSlot {o = obj};
//            }
//        }
//
//        public unsafe void ToTypedReferencePrimitive(TypedRefPtr typedRef)
//        {
//            *(TypedReference*) typedRef = __makeref(u4);
//        }
//
//        public unsafe void ToTypedReferenceObject(TypedRefPtr typedRef, Type type)
//        {
//            if(o is ValueType && type.IsValueType)
//                TypedReferenceHelpers.UnboxTypedRef(o, typedRef);
//            else
//                *(TypedReference*) typedRef = __makeref(o);
//        }
//
//        public unsafe object ToObject(Type type)
//        {
//            if(type.IsEnum)
//                return Enum.ToObject(type, ToObject(Enum.GetUnderlyingType(type)));
//
//            switch(Type.GetTypeCode(type))
//            {
//                case TypeCode.Byte:
//                    return u1;
//                case TypeCode.SByte:
//                    return (sbyte) u1;
//                case TypeCode.Boolean:
//                    return u1 != 0;
//
//                case TypeCode.UInt16:
//                    return u2;
//                case TypeCode.Int16:
//                    return (short) u2;
//                case TypeCode.Char:
//                    return (char) u2;
//
//                case TypeCode.UInt32:
//                    return u4;
//                case TypeCode.Int32:
//                    return (int) u4;
//
//                case TypeCode.UInt64:
//                    return u8;
//                case TypeCode.Int64:
//                    return (long) u8;
//
//                case TypeCode.Single:
//                    return r4;
//                case TypeCode.Double:
//                    return r8;
//
//                default:
//                    if(type.IsPointer)
//                        return Pointer.Box((void*) u8, type);
//                    if(type == typeof(IntPtr))
//                        return Platform.x64 ? new IntPtr((long) u8) : new IntPtr((int) u4);
//                    if(type == typeof(UIntPtr))
//                        return Platform.x64 ? new UIntPtr(u8) : new UIntPtr(u4);
//                    return ValueTypeBox.Unbox(o);
//            }
//        }
    }
}