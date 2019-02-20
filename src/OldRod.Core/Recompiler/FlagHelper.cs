namespace OldRod.Core.Recompiler
{
    public static class FlagHelper
    {
        private static readonly byte FL_OVERFLOW;
        private static readonly byte FL_CARRY;
        private static readonly byte FL_ZERO;
        private static readonly byte FL_SIGN;
        private static readonly byte FL_UNSIGNED;
        private static readonly byte FL_BEHAV1;
        private static readonly byte FL_BEHAV2;
        private static readonly byte FL_BEHAV3;

        static FlagHelper()
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
    }
}