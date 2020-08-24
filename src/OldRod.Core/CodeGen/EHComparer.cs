using System.Collections.Generic;
using AsmResolver.DotNet.Code.Cil;

namespace OldRod.Core.CodeGen
{
    public class EHComparer : IComparer<CilExceptionHandler>
    {
        public int Compare(CilExceptionHandler x, CilExceptionHandler y)
        {
            if (x == null || y == null)
                return 0;

            // Make sure x starts at a later point than y.
            if (x.TryStart.Offset < y.TryStart.Offset)
                return -Compare(y, x);
            
            // If x also ends earlier, then we have a nested EH. x should be prioritized. 
            if (x.TryEnd.Offset < y.TryEnd.Offset)
                return -1;

            // If x does not have the same try block, then we know that x appears after y.
            if (x.TryEnd.Offset != y.TryEnd.Offset)
                return 1;
            
            // Prioritize handler that starts earlier. 
            return x.HandlerStart.Offset.CompareTo(y.HandlerStart.Offset);
        }
        
    }
}