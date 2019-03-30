﻿// Project OldRod - A KoiVM devirtualisation utility.
// Copyright (C) 2019 Washi
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using AsmResolver;
using AsmResolver.Net.Metadata;
using OldRod.CommandLine;
using OldRod.Core;
using OldRod.Core.Recompiler;
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
            using (var image = new Bitmap(Image.FromStream(stream), 43, 25))
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

            var tui = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location);
            var core = FileVersionInfo.GetVersionInfo(typeof(ILogger).Assembly.Location);
            var pipeline = FileVersionInfo.GetVersionInfo(typeof(Devirtualiser).Assembly.Location);
            var asmres = FileVersionInfo.GetVersionInfo(typeof(WindowsAssembly).Assembly.Location);
            var rivers = FileVersionInfo.GetVersionInfo(typeof(Graph).Assembly.Location);
            
            PrintAlignedLine("Catching Koi fish (or magikarps if you will) from the .NET binary!");
            Console.CursorTop++;
            PrintAlignedLine("KoiVM devirtualisation utility");
            PrintAlignedLine("TUI Version:         " + tui.FileVersion);
            PrintAlignedLine("Recompiler Version:  " + core.FileVersion);
            PrintAlignedLine("Pipelining Version:  " + pipeline.FileVersion);
            PrintAlignedLine("AsmResolver Version: " + asmres.FileVersion);
            PrintAlignedLine("Rivers Version:      " + rivers.FileVersion);
            PrintAlignedLine("Copyright:           " + tui.LegalCopyright);
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
            var consoleLogger = new FilteredLogger(new ConsoleLogger());
            FileOutputLogger fileLogger = null;
            
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
                    consoleLogger.IncludeDebug = result.Flags.Contains(CommandLineSwitches.VerboseOutput);
                    var options = GetDevirtualisationOptions(result);
                    options.OutputOptions.EnsureDirectoriesExist();

                    ILogger logger = consoleLogger;
                    if (result.Flags.Contains(CommandLineSwitches.OutputLogFile))
                    {
                        fileLogger = new FileOutputLogger(Path.Combine(options.OutputOptions.RootDirectory, "report.log"));
                        logger = new LoggerCollection {logger, fileLogger};
                    }

                    var devirtualiser = new Devirtualiser(logger);
                    devirtualiser.Devirtualise(options);
                }
            }
            catch (CommandLineParseException ex)
            {
                consoleLogger.Error(Tag, ex.Message);
                consoleLogger.Log(Tag, "Use -h for help.");
            }
#if !DEBUG
            catch (Exception ex)
            {
                consoleLogger.Error(Tag, "Something went wrong! Try the latest version or report a bug at the repository.");
                if (consoleLogger.IncludeDebug)
                {
                    consoleLogger.Error(Tag, ex.ToString());
                }
                else
                {
                    while (ex != null)
                    {
                        consoleLogger.Error(Tag, ex.Message);
                        ex = ex.InnerException;
                    }
                }
            }
#endif
            finally
            {
                fileLogger?.Dispose();
            }

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
                OutputOptions =
                {
                    DumpControlFlowGraphs = result.Flags.Contains(CommandLineSwitches.DumpCfg),
                    DumpAllControlFlowGraphs = result.Flags.Contains(CommandLineSwitches.DumpAllCfg),
                    DumpDisassembledIL = result.Flags.Contains(CommandLineSwitches.DumpIL),
                    DumpRecompiledCil =  result.Flags.Contains(CommandLineSwitches.DumpCIL),
                },
                OverrideVMEntryToken = result.Options.ContainsKey(CommandLineSwitches.OverrideVMEntry),
                OverrideVMConstantsToken = result.Options.ContainsKey(CommandLineSwitches.OverrideVMConstants),
                KoiStreamName = result.GetOptionOrDefault(CommandLineSwitches.KoiStreamName),
                RenameConstants = result.Flags.Contains(CommandLineSwitches.RenameConstants),
                RuntimeFile = result.GetOptionOrDefault(CommandLineSwitches.RuntimeLibFileName)
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