namespace OldRod.Core
{
    public class EmptyLogger : ILogger
    {
        public static EmptyLogger Instance
        {
            get;
        } = new EmptyLogger();

        private EmptyLogger()
        {
        }
        
        public void Debug(string tag, string message)
        {
        }

        public void Log(string tag, string message)
        {
        }

        public void Warning(string tag, string message)
        {
        }

        public void Error(string tag, string message)
        {
        }
    }
}