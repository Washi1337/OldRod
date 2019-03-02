using System;

namespace OldRod.Core.Architecture
{
    [Flags]
    public enum VMFlags
    {
        OVERFLOW = 0,
        CARRY = 1,
        ZERO = 2,
        SIGN = 3,
        UNSIGNED = 4,
        BEHAV1 = 5,
        BEHAV2 = 6,
        BEHAV3 = 7,

        Max
    }
}