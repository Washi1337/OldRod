using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using AsmResolver;
using AsmResolver.Net.Metadata;
using OldRod.CommandLine;
using OldRod.Core;
using OldRod.Pipeline;
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
            using (var image = new Bitmap(Image.FromStream(stream), 40, 25))
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
            Console.CursorTop++;
            PrintAlignedLine("This program comes with ABSOLUTELY NO WARRANTY.");
            PrintAlignedLine("This is free software, and you are welcome to redistribute it");
            PrintAlignedLine("under the conditions of GPLv3.");
            
            Console.CursorTop = next;
            Console.CursorLeft = 0;
            Console.WriteLine();
        }

        private static void PrintAlignedLine(string message)
        {
            Console.CursorLeft = 45;
            Console.Write(message);
            Console.CursorTop++;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("   OldRod.exe [options] <input-file> [options]");
            Console.WriteLine();
            Console.WriteLine("Available options:");
            foreach (var @switch in CommandLineSwitches.AllSwitches.OrderBy(x => x.Identifiers.First()))
            {
                Console.Write("   -" + string.Join(" -", @switch.Identifiers.OrderBy(x => x.Length)).PadRight(25));
                Console.WriteLine(@switch.Description);
            }

            Console.WriteLine();
            Console.WriteLine("Examples: ");
            Console.WriteLine("   OldRod.exe -v C:\\Path\\To\\Input\\File.exe");
            Console.WriteLine("   OldRod.exe C:\\Path\\To\\Input\\File.exe -o C:\\OutputDirectory");
            Console.WriteLine();
        }
        
        public static void Main(string[] args)
        {
            PrintAbout();

            bool pauseOnExit = true;
            var logger = new FilteredLogger(new ConsoleLogger());

            var parser = new CommandLineParser();
            foreach (var @switch in CommandLineSwitches.AllSwitches)
                parser.AddSwitch(@switch);

            try
            {
                var result = parser.Parse(args);
                pauseOnExit = !result.Flags.Contains(CommandLineSwitches.NoPause);
                
                if (result.Flags.Contains(CommandLineSwitches.Help))
                {
                    PrintHelp();
                }
                else
                {
                    logger.IncludeDebug = result.Flags.Contains(CommandLineSwitches.VerboseOutput);

                    var options = GetDevirtualisationOptions(result);
                    
                    var devirtualiser = new Devirtualiser(logger);                    
                    devirtualiser.Devirtualise(options);
                }
            }
            catch (CommandLineParseException ex)
            {
                logger.Error(Tag, ex.Message);
                logger.Log(Tag, "Use -h for help.");
            }
            #if !DEBUG
            catch (Exception ex)
            {
                logger.Error(Tag, "Something went wrong! Try latest version or report a bug at the repository.");
                logger.Error(Tag, ex.Message);
            }
            #endif

            if (pauseOnExit)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private static DevirtualisationOptions GetDevirtualisationOptions(CommandParseResult result)
        {
            string filePath = result.FilePath
                              ?? throw new CommandLineParseException("No input file path specified.");
            
            string outputDirectory = result.GetOptionOrDefault(CommandLineSwitches.OutputDirectory,
                Path.Combine(Path.GetDirectoryName(filePath), "Devirtualised"));

            var options = new DevirtualisationOptions(filePath, outputDirectory)
            {
                DumpControlFlowGraphs = result.Flags.Contains(CommandLineSwitches.DumpCfg),
                DumpDisassembledIL = result.Flags.Contains(CommandLineSwitches.DumpIL),
                OverrideVMEntryToken = result.Options.ContainsKey(CommandLineSwitches.OverrideVMEntry),
                OverrideVMConstantsToken = result.Options.ContainsKey(CommandLineSwitches.OverrideVMConstants),
                KoiStreamName = result.GetOptionOrDefault(CommandLineSwitches.KoiStreamName)
            };

            if (options.OverrideVMEntryToken)
            {
                options.VMEntryToken = new MetadataToken(uint.Parse(
                    result.GetOptionOrDefault(CommandLineSwitches.OverrideVMEntry), NumberStyles.HexNumber));
            }

            if (options.OverrideVMConstantsToken)
            {
                options.VMConstantsToken = new MetadataToken(uint.Parse(
                    result.GetOptionOrDefault(CommandLineSwitches.OverrideVMConstants), NumberStyles.HexNumber));
            }

            if (result.Options.ContainsKey(CommandLineSwitches.IgnoreExport))
            {
                var ignoredExports = result.GetOptionOrDefault(CommandLineSwitches.IgnoreExport)
                    .Split(',')
                    .Select(uint.Parse)
                    .ToArray();

                foreach (uint ignoredExport in ignoredExports)
                    options.IgnoredExports.Add(ignoredExport);
            }

            return options;
        }
    }
}