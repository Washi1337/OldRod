using System;
using OldRod.Core;
using OldRod.Pipeline;

namespace OldRod
{
    public class ConsoleLogger : ILogger
    {
        private static void WriteMessage(ConsoleColor color, string tag, string message)
        {
            var previous = Console.ForegroundColor;
            Console.ForegroundColor = color;
            var time = DateTime.Now;
            Console.WriteLine($"{time.Hour:00}:{time.Minute:00}:{time.Second:00}.{time.Millisecond:000} [{tag}]: {message}");
            Console.ForegroundColor = previous;
        }

        public void Debug(string tag, string message)
        {
            WriteMessage(ConsoleColor.DarkGray, tag, message);
        }

        public void Log(string tag, string message)
        {
            WriteMessage(ConsoleColor.Gray, tag, message);
        }

        public void Warning(string tag, string message)
        {
            WriteMessage(ConsoleColor.Yellow, tag, message);
        }

        public void Error(string tag, string message)
        {
            WriteMessage(ConsoleColor.Red, tag, message);
        }
    }
}