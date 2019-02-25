using System;

namespace OldRod.Core.CodeGen
{
    public class CilCodeGeneratorException : Exception
    {
        public CilCodeGeneratorException(string message) 
            : base(message)
        {
        }

        public CilCodeGeneratorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}