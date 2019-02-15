using System;

namespace OldRod.CommandLine
{
    public class CommandLineParseException : Exception
    {
        public CommandLineParseException(string message) 
            : base(message)
        {
        }
    }
}