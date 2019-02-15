using System;
using System.Drawing;
using System.IO;
using AsmResolver;
using OldRod.CommandLine;
using OldRod.Core;
using OldRod.Transpiler;
using Rivers;

namespace OldRod
{
    internal class Program
    {
        public const string Tag = "TUI";
        
        private static void PrintAbout()
        {
            int top = Console.CursorTop;
            using (var stream = typeof(Program).Assembly.GetManifestResourceStream("OldRod.Resources.magikarp.png"))
//            using (var stream = File.OpenRead(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PGL-143.png")))
            using (var image = new Bitmap(Image.FromStream(stream), 35, 25))
            {
                var ascii = new ConsoleAsciiImage(image);
                ascii.PrintAscii(true);
            }

            int next = Console.CursorTop;

            Console.CursorTop = top + 5;
            
            #if DEBUG
            PrintAlignedLine("Project Old Rod (DEBUG)");
            #else
            PrintAlignedLine("Project Old Rod");
            #endif
            
            PrintAlignedLine("Catching Koi fish (or magikarps if you will) from the .NET binary!");
            Console.CursorTop++;
            PrintAlignedLine("KoiVM devirtualisation utility");
            PrintAlignedLine("TUI Version:         " + typeof(Program).Assembly.GetName().Version);
            PrintAlignedLine("Recompiler Version:  " + typeof(ILogger).Assembly.GetName().Version);
            PrintAlignedLine("Pipelining Version:  " + typeof(Devirtualiser).Assembly.GetName().Version);
            PrintAlignedLine("AsmResolver Version: " + typeof(WindowsAssembly).Assembly.GetName().Version);
            PrintAlignedLine("Rivers Version:      " + typeof(Graph).Assembly.GetName().Version);
            PrintAlignedLine("Copyright:           Washi 2019 - https://rtn-team.cc/");
            PrintAlignedLine("GIT + issue tracker: https://github.com/Washi1337/OldRod");
            
            Console.CursorTop = next;
            Console.CursorLeft = 0;
            Console.WriteLine();
        }

        private static void PrintAlignedLine(string message)
        {
            Console.CursorLeft = 40;
            Console.Write(message);
            Console.CursorTop++;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("");
        }
        
        public static void Main(string[] args)
        {
            PrintAbout();
            var logger = new FilteredLogger(new ConsoleLogger());

            try
            {
                var parser = new CommandLineParser
                {
                    Flags = {'v', 'h', 'c', 'd'},
                    Options = {'o'}
                };

                var result = parser.Parse(args);
                
                string filePath = result.FilePath;
                logger.IncludeDebug = result.Flags.Contains('v');
                
                var devirtualiser = new Devirtualiser(logger);
                string outputDirectory =
                    result.GetOptionOrDefault('o', Path.Combine(Path.GetDirectoryName(filePath), "Devirtualised"));
                
                devirtualiser.Devirtualise(new DevirtualisationOptions(filePath, outputDirectory)
                {
                    DumpControlFlowGraphs = result.Flags.Contains('c'),
                    DumpDisassembledIL = result.Flags.Contains('d'),
                });
            }
            catch (CommandLineParseException ex)
            {
                logger.Error(Tag, ex.Message);
                logger.Log(Tag, "Use -h for help.");
            }
            #if !DEBUG
            catch (Exception ex)
            {
                logger.Log(Tag, "Something went wrong! Try latest version or report a bug at the repository" + ex.Message);
            }
            #endif
            finally
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }
}