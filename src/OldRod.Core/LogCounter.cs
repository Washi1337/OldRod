namespace OldRod.Core
{
    public class LogCounter : ILogger
    {
        public int DebugMessages2
        {
            get;
            private set;
        }
        public int DebugMessages
        {
            get;
            private set;
        }

        public int Messages
        {
            get;
            private set;
        }

        public int Warnings
        {
            get;
            private set;
        }

        public int Errors
        {
            get;
            private set;
        }

        public void Debug2(string tag, string message)
        {
            DebugMessages2++;
        }

        public void Debug(string tag, string message)
        {
            DebugMessages++;
        }

        public void Log(string tag, string message)
        {
            Messages++;
        }

        public void Warning(string tag, string message)
        {
            Warnings++;
        }

        public void Error(string tag, string message)
        {
            Errors++;
        }
    }
}