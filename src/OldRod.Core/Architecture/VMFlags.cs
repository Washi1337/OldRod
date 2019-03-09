using System;

namespace OldRod.Core.Architecture
{
    [Flags]
    public enum VMFlags
    {
        OVERFLOW = 1,
        CARRY = 2,
        ZERO = 4,
        SIGN = 8,
        UNSIGNED = 16,
        BEHAV1 = 32,
        BEHAV2 = 64,
        BEHAV3 = 128,

        Max
    }
}