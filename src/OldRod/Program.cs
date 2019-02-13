using System;
using System.Drawing;
using AsmResolver;
using OldRod.Core;
using OldRod.Transpiler;
using Rivers;

namespace OldRod
{
    internal class Program
    {
        private static void PrintAbout()
        {
            int top = Console.CursorTop;
            using (var stream = typeof(Program).Assembly.GetManifestResourceStream("OldRod.Resources.magikarp.png"))
            using (var image = new Bitmap(Image.FromStream(stream), 30,25))
            {
                var ascii = new AsciiImage(image);
                ascii.PrintAscii(true);
            }

            int next = Console.CursorTop;

            Console.CursorTop = top + 5;
            PrintAlignedLine("Project Old Rod");
            PrintAlignedLine("Catching Koi fish (Magikarps) from the .NET binary!");
            Console.CursorTop++;
            PrintAlignedLine("KoiVM devirtualisation tool");
            PrintAlignedLine("TUI Version:    " + typeof(Program).Assembly.GetName().Version);
            PrintAlignedLine("Core Version:   " + typeof(ILogger).Assembly.GetName().Version);
            PrintAlignedLine("Devirt Version: " + typeof(Devirtualiser).Assembly.GetName().Version);
            PrintAlignedLine("AsmRes Version: " + typeof(WindowsAssembly).Assembly.GetName().Version);
            PrintAlignedLine("Rivers Version: " + typeof(Graph).Assembly.GetName().Version);
            PrintAlignedLine("Copyright:      Washi 2019 - https://rtn-team.cc/");
            PrintAlignedLine("Repository:     https://github.com/Washi1337/OldRod");
            
            Console.CursorTop = next;
            Console.CursorLeft = 0;
        }

        private static void PrintAlignedLine(string message)
        {
            Console.CursorLeft = 35;
            Console.Write(message);
            Console.CursorTop++;
        }
        public static void Main(string[] args)
        {
            PrintAbout();

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: OldRod.exe <path>");
                Console.ReadKey();
                return;
            }
            
            string filePath = args[0].Replace("\"", "");

            var devirtualiser = new Devirtualiser(new FilteredLogger(new ConsoleLogger())
            {
                IncludeDebug = false
            });
            devirtualiser.Devirtualise(filePath);
        }
    }
}