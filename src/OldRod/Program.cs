// Project OldRod - A KoiVM devirtualisation utility.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;
using OldRod.CommandLine;
using OldRod.Core;
using OldRod.Json;
using OldRod.Pipeline;
using Rivers;

namespace OldRod
{
    internal class Program
    {
        public const string Tag = "TUI";
        
        private static void PrintAbout()
        {
            if (Console.BufferHeight > 43)
                WriteAlignedAbout();
            else
                WriteFallbackAbout();
        }

        private static void WriteFallbackAbout()
        {  
#if DEBUG
            Console.WriteLine("Project Old Rod (DEBUG)");
#else
            Console.WriteLine("Project Old Rod");
#endif
            Console.WriteLine("Catching Koi fish (or magikarps if you will) from the .NET binary!");
            
            var tui = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location);
            var core = FileVersionInfo.GetVersionInfo(typeof(ILogger).Assembly.Location);
            var pipeline = FileVersionInfo.GetVersionInfo(typeof(Devirtualiser).Assembly.Location);
            var asmres = FileVersionInfo.GetVersionInfo(typeof(AssemblyDefinition).Assembly.Location);
            var rivers = FileVersionInfo.GetVersionInfo(typeof(Graph).Assembly.Location);

            Console.WriteLine();
            Console.WriteLine("KoiVM devirtualisation utility");
            Console.WriteLine("TUI Version:         " + tui.FileVersion);
            Console.WriteLine("Recompiler Version:  " + core.FileVersion);
            Console.WriteLine("Pipelining Version:  " + pipeline.FileVersion);
            Console.WriteLine("AsmResolver Version: " + asmres.FileVersion);
            Console.WriteLine("Rivers Version:      " + rivers.FileVersion);
            Console.WriteLine("Copyright:           " + tui.LegalCopyright);
            Console.WriteLine("GIT + issue tracker: https://github.com/Washi1337/OldRod");
            Console.WriteLine();
            Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY.");
            Console.WriteLine("This is free software, and you are welcome to redistribute it");
            Console.WriteLine("under the conditions of GPLv3.");
        }

        private static void WriteAlignedAbout()
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
            var asmres = FileVersionInfo.GetVersionInfo(typeof(AssemblyDefinition).Assembly.Location);
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
                var identifiers = @switch.Identifiers
                    .OrderByDescending(x => x.Length)
                    .ToArray();
                
                Console.Write("   -" + identifiers[0].PadRight(25));
                Console.WriteLine(@switch.Description);
                
                for (int i = 1; i < identifiers.Length;i++)
                    Console.WriteLine("   -" + identifiers[i]);
                Console.WriteLine();
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
            var counter = new LogCounter();
            
            var loggers = new LoggerCollection {consoleLogger, counter};
            FileOutputLogger fileLogger = null;
            
            var parser = new CommandLineParser();
            foreach (var @switch in CommandLineSwitches.AllSwitches)
                parser.AddSwitch(@switch);

            try
            {
                var result = parser.Parse(args);
                if (result.Flags.Contains(CommandLineSwitches.EnableTroublenoobing))
                    throw new DevirtualisationException("Magikarp uses Splash! It was not very effective...");

                pauseOnExit = !result.Flags.Contains(CommandLineSwitches.NoPause);

                if (result.Flags.Contains(CommandLineSwitches.Help))
                {
                    PrintHelp();
                }
                else
                {
                    consoleLogger.IncludeDebug = result.Flags.Contains(CommandLineSwitches.VerboseOutput)
                                                 || result.Flags.Contains(CommandLineSwitches.VeryVerboseOutput);
                    consoleLogger.IncludeDebug2 = result.Flags.Contains(CommandLineSwitches.VeryVerboseOutput);

                    var options = GetDevirtualisationOptions(result);
                    options.OutputOptions.EnsureDirectoriesExist();

                    if (result.Flags.Contains(CommandLineSwitches.OutputLogFile))
                    {
                        fileLogger =
                            new FileOutputLogger(Path.Combine(options.OutputOptions.RootDirectory, "report.log"));
                        loggers.Add(fileLogger);
                    }

                    if (result.Flags.Contains(CommandLineSwitches.SalvageData))
                    {
                        loggers.Warning(Tag,
                            "Salvage mode is enabled. Output files might not be an accurate representation of the original binary.");
                    }

                    var devirtualiser = new Devirtualiser(loggers);
                    devirtualiser.Devirtualise(options);
                }
            }
            catch (CommandLineParseException ex)
            {
                consoleLogger.Error(Tag, ex.Message);
                consoleLogger.Log(Tag, "Use -h for help.");
            }
            catch (Exception ex) when (!Debugger.IsAttached) 
            {
                consoleLogger.Error(Tag, "Something went wrong! Try the latest version or report a bug at the repository.");
                if (consoleLogger.IncludeDebug)
                {
                    loggers.Error(Tag, ex.ToString());
                }
                else
                {
                    PrintExceptions(new LoggerCollection {consoleLogger, counter}, new[] {ex});
                    fileLogger?.Error(Tag, ex.ToString());
                    consoleLogger.Error(Tag, "Use --verbose or inspect the full report.log using --log-file for more details.");
                }
            }
            finally
            {
                loggers.Log(Tag, $"Process finished with {counter.Warnings} warnings and {counter.Errors} errors.");
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
                EnableSalvageMode = result.Flags.Contains(CommandLineSwitches.SalvageData),
                KoiStreamName = result.GetOptionOrDefault(CommandLineSwitches.KoiStreamName),
                KoiStreamDataFile = result.GetOptionOrDefault(CommandLineSwitches.KoiStreamData),
                RenameSymbols = result.Flags.Contains(CommandLineSwitches.RenameConstants),
                RuntimeFile = result.GetOptionOrDefault(CommandLineSwitches.RuntimeLibFileName),
                NoExportMapping = result.Flags.Contains(CommandLineSwitches.NoExportMapping),
                IgnoreInvalidMD = result.Flags.Contains(CommandLineSwitches.IgnoreInvalidMD),
            };

            if (result.Flags.Contains(CommandLineSwitches.ForceEmbeddedRuntimeLib))
                options.RuntimeFile = options.InputFile;

            if (result.Options.ContainsKey(CommandLineSwitches.OverrideVMEntry))
            {
                options.VMEntryToken = new MetadataToken(uint.Parse(
                    result.GetOptionOrDefault(CommandLineSwitches.OverrideVMEntry), NumberStyles.HexNumber));
            }

            if (result.Options.ContainsKey(CommandLineSwitches.OverrideVMConstants))
            {
                options.VMConstantsToken = new MetadataToken(uint.Parse(
                    result.GetOptionOrDefault(CommandLineSwitches.OverrideVMConstants), NumberStyles.HexNumber));
            }
            
            if (result.Options.ContainsKey(CommandLineSwitches.IgnoreExports))
            {
                var ignoredExports = result.GetOptionOrDefault(CommandLineSwitches.IgnoreExports)
                    .Split(',')
                    .Select(uint.Parse)
                    .ToArray();

                var selection = new ExclusionIdSelection();
                selection.ExcludedIds.UnionWith(ignoredExports);
                options.SelectedExports = selection;
            }

            if (result.Options.ContainsKey(CommandLineSwitches.OnlyExports))
            {
                if (options.SelectedExports != IdSelection.All)
                {
                    throw new CommandLineParseException(
                        "Cannot use the --ignore-exports and --only-exports command-line switches at the same time.");
                }

                var ignoredExports = result.GetOptionOrDefault(CommandLineSwitches.OnlyExports)
                    .Split(',')
                    .Select(uint.Parse)
                    .ToArray();

                var selection = new IncludedIdSelection();
                selection.IncludedIds.UnionWith(ignoredExports);
                options.SelectedExports = selection;
            }

            if (result.Options.ContainsKey(CommandLineSwitches.IgnoreMethods))
            {
                var ignoredMethods = result.GetOptionOrDefault(CommandLineSwitches.IgnoreMethods)
                    .Split(',')
                    .Select(uint.Parse)
                    .ToArray();

                var selection = new ExclusionIdSelection();
                selection.ExcludedIds.UnionWith(ignoredMethods);
                options.SelectedMethods = selection;
            }

            if (result.Options.ContainsKey(CommandLineSwitches.OnlyMethods))
            {
                if (options.SelectedMethods != IdSelection.All)
                {
                    throw new CommandLineParseException(
                        "Cannot use the --ignore-methods and --only-methods command-line switches at the same time.");
                }

                var ignoredMethods = result.GetOptionOrDefault(CommandLineSwitches.OnlyMethods)
                    .Split(',')
                    .Select(uint.Parse)
                    .ToArray();

                var selection = new IncludedIdSelection();
                selection.IncludedIds.UnionWith(ignoredMethods);
                options.SelectedMethods = selection;
            }

            if (result.Options.TryGetValue(CommandLineSwitches.ConfigurationFile, out string configFile))
            {
                configFile = configFile.Replace("\"", "");
                var jsonConfig = ConstantsConfiguration.FromFile(configFile);
                options.Constants = jsonConfig.CreateVmConstants();
            }

            if (result.Options.TryGetValue(CommandLineSwitches.MaxSimplificationPasses, out string maxPasses))
            {
                options.MaxSimplificationPasses = int.Parse(maxPasses);
            }
            
            return options;
        }

        private static void PrintExceptions(ILogger logger, IEnumerable<Exception> exceptions, int level = 0)
        {
            foreach (var exception in exceptions)
            {
                logger.Error(Tag, $"{new string(' ', level * 3)}{exception.GetType().Name}: {exception.Message}");
                
                if (exception is AggregateException a)
                    PrintExceptions(logger, a.InnerExceptions, level + 1);
                else if (exception.InnerException != null)
                    PrintExceptions(logger, new[] {exception.InnerException}, level + 1);
            }
        }
        
    }
}