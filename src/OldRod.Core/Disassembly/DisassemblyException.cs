using System;

namespace OldRod.Core.Disassembly
{
    public class DisassemblyException : Exception
    {
        public DisassemblyException(string message) 
            : base(message)
        {
        }

        public DisassemblyException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}