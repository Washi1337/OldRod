using System;
using System.IO;

namespace OldRod.Core
{
    public class FileOutputLogger : ILogger, IDisposable
    {
        private readonly StreamWriter _writer;

        public FileOutputLogger(string path)
        {
            _writer = new StreamWriter(path);
        }

        private void WriteMessage(string severity, string tag, string message)
        {
            var time = DateTime.Now;
            _writer.WriteLine("{0,5}: {1:00}:{2:00}:{3:00}.{4:000} [{5}]: {6}", 
                severity, 
                time.Hour, 
                time.Minute, 
                time.Second,
                time.Millisecond,
                tag,
                message);
        }

        public void Debug(string tag, string message)
        {
            WriteMessage("DEBUG", tag, message);
        }

        public void Log(string tag, string message)
        {
            WriteMessage("LOG", tag, message);
        }

        public void Warning(string tag, string message)
        {
            WriteMessage("WARNING", tag, message);
        }

        public void Error(string tag, string message)
        {
            WriteMessage("ERROR", tag, message);
        }

        public void Dispose()
        {
            _writer.Flush();
            _writer.Dispose();
        }
    }
}