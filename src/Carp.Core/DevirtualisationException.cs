using System;

namespace Carp.Core
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