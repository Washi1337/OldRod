using System.Collections.Generic;

namespace OldRod.Core
{
    public class LoggerCollection : List<ILogger>, ILogger
    {
        public void Debug2(string tag, string message)
        {
            foreach (var logger in this)
                logger.Debug2(tag, message);
        }

        public void Debug(string tag, string message)
        {
            foreach (var logger in this)
                logger.Debug(tag, message);
        }

        public void Log(string tag, string message)
        {
            foreach (var logger in this)
                logger.Log(tag, message);
        }

        public void Warning(string tag, string message)
        {
            foreach (var logger in this)
                logger.Warning(tag, message);
        }

        public void Error(string tag, string message)
        {
            foreach (var logger in this)
                logger.Error(tag, message);
        }
    }
}