using System;

namespace OldRod.Core.Memory
{
    public class FrameLayoutDetectionException : Exception
    {
        public FrameLayoutDetectionException(string message) 
            : base(message)
        {
        }

        public FrameLayoutDetectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}