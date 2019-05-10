using System;

namespace OldRod.Core.Emulation
{
    public class EmulationException : Exception
    {
        public EmulationException()
        {
        }

        public EmulationException(string message)
            : base(message)
        {
        }

        public EmulationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}