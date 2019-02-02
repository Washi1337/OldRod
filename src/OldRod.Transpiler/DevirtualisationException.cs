using System;

namespace OldRod.Transpiler
{
    public class DevirtualisationException : Exception
    {
        public DevirtualisationException()
        {
        }

        public DevirtualisationException(string message) 
            : base(message)
        {
        }

        public DevirtualisationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}