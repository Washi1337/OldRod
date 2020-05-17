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
using System.IO;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.Net;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Emit;
using AsmResolver.Net.Metadata;
using OldRod.Core;
using OldRod.Core.Architecture;
using OldRod.Pipeline.Stages;
using OldRod.Pipeline.Stages.AstBuilding;
using OldRod.Pipeline.Stages.CleanUp;
using OldRod.Pipeline.Stages.CodeAnalysis;
using OldRod.Pipeline.Stages.ConstantsResolution;
using OldRod.Pipeline.Stages.KoiStreamParsing;
using OldRod.Pipeline.Stages.OpCodeResolution;
using OldRod.Pipeline.Stages.Recompiling;
using OldRod.Pipeline.Stages.VMCodeRecovery;
using OldRod.Pipeline.Stages.VMMethodDetection;

namespace OldRod.Pipeline
{
    public class Devirtualiser
    {
        private const string Tag = "Main";
        
        private static ISet<string> RuntimeAssemblyNames = new HashSet<string>
        {
            "Virtualization",
            "KoiVM.Runtime",
        };

        public Devirtualiser(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Stages = new List<IStage>
            {
                new ConstantsResolutionStage(),
                new KoiStreamParserStage(),
                new VMMethodDetectionStage(),
                new OpCodeResolutionStage(),
                new VMCodeRecoveryStage(),
                new CodeAnalysisStage(),
                new AstBuilderStage(),
                new RecompilerStage(),
                new CleanUpStage()
            };
        }

        public ILogger Logger
        {
            get;
        }

        public IList<IStage> Stages
        {
            get;
        }

        public void Devirtualise(DevirtualisationOptions options)
        {
            Logger.Log(Tag, "Started devirtualisation.");

            // Create output directory.
            options.OutputOptions.EnsureDirectoriesExist();

            var context = CreateDevirtualisationContext(options);

            // Run pipeline.
            RunPipeline(context);

            // Unlock images.
            Logger.Log(Tag, $"Commiting changes to metadata streams...");
            context.TargetModule.Header.UnlockMetadata();

            bool rebuildRuntimeImage = options.RenameSymbols && !options.RuntimeIsEmbedded;
            if (rebuildRuntimeImage)
                context.RuntimeModule.Header.UnlockMetadata();

            RemoveFinalTraces(options, context);

            // Rebuild.
            Rebuild(options, context, rebuildRuntimeImage);

            Logger.Log(Tag, $"Finished. All fish were caught and served!");
        }

        private DevirtualisationContext CreateDevirtualisationContext(DevirtualisationOptions options)
        {
            // Open target file.
            Logger.Log(Tag, $"Opening target file {options.InputFile}...");
            var inputModule = ModuleDefinition.FromFile(options.InputFile);
            var header = inputModule.NetDirectory?.MetadataHeader;

            if (header == null)
                throw new BadImageFormatException("Assembly does not contain a valid .NET header.");            

            // Hook into md stream parser.
            var parser = new KoiVmAwareStreamParser(options.KoiStreamName, Logger);
            if (options.OverrideKoiStreamData)
            {
                string path = Path.IsPathRooted(options.KoiStreamDataFile)
                    ? options.KoiStreamDataFile
                    : Path.Combine(Path.GetDirectoryName(options.InputFile), options.KoiStreamDataFile);
                
                Logger.Log(Tag, $"Opening external Koi stream data file {path}...");
                parser.ReplacementData = File.ReadAllBytes(path);
                
                var streamHeader = header.StreamHeaders.FirstOrDefault(h => h.Name == options.KoiStreamName);
                if (streamHeader == null)
                {
                    streamHeader = new MetadataStreamHeader(options.KoiStreamName)
                    {
                        Stream = KoiStream.FromReadingContext(
                            new ReadingContext() {Reader = new MemoryStreamReader(parser.ReplacementData)}, Logger)
                    };
                    header.StreamHeaders.Add(streamHeader);
                }
            }

            header.StreamParser = parser;

            // Ignore invalid / encrypted method bodies when specified.
            if (options.IgnoreInvalidMethodBodies)
            {
                var table = header.GetStream<TableStream>().GetTable<MethodDefinitionTable>();
                table.MethodBodyReader = new DefaultMethodBodyReader {ThrowOnInvalidMethodBody = false};
            }

            // Lock image and set custom md resolver.
            var image = header.LockMetadata();

            var runtimeImage = ResolveRuntimeImage(options, image);

            return new DevirtualisationContext(options, image, runtimeImage, Logger);

        }

        private MetadataImage ResolveRuntimeImage(DevirtualisationOptions options, MetadataImage image)
        {
            MetadataImage runtimeImage = null;

            if (options.AutoDetectRuntimeFile)
            {
                Logger.Debug(Tag, "Attempting to autodetect location of the runtime library...");
                var runtimeAssemblies = image.Assembly.AssemblyReferences
                    .Where(r => RuntimeAssemblyNames.Contains(r.Name))
                    .ToArray();
                
                switch (runtimeAssemblies.Length)
                {
                    case 0:
                        // No assembly references detected, default to embedded.
                        Logger.Debug(Tag, "No references found to a runtime library.");
                        options.RuntimeFile = options.InputFile;
                        break;
                    case 1:
                        // A single assembly reference with a known KoiVM runtime library name was found.
                        Logger.Debug(Tag, $"Reference to runtime library detected ({runtimeAssemblies[0].Name}).");
                        options.RuntimeFile =
                            Path.Combine(Path.GetDirectoryName(options.InputFile), runtimeAssemblies[0].Name + ".dll");
                        break;
                    default:
                        // Multiple assembly references with a known KoiVM runtime library name were found.
                        // Report to the user that they have to choose which one to use. 
                        throw new DevirtualisationException(
                            "Multiple runtime assembly reference detected. "
                            + "Please specify the location of the runtime assembly to use in the devirtualizer options.");
                }
            }

            if (options.RuntimeIsEmbedded)
            {
                // Runtime is embedded into the assembly, so they share the same metadata image.
                Logger.Log(Tag, "Runtime is embedded in the target assembly.");
                runtimeImage = image;
            }
            else if (options.RuntimeFile != null)
            {
                // Resolve runtime library.
                Logger.Log(Tag, $"Opening external runtime library located at {options.RuntimeFile}...");

                string runtimePath = Path.IsPathRooted(options.RuntimeFile)
                    ? options.RuntimeFile
                    : Path.Combine(Path.GetDirectoryName(options.InputFile), options.RuntimeFile);
                var runtimeAssembly = WindowsAssembly.FromFile(runtimePath);
                runtimeImage = runtimeAssembly.NetDirectory.MetadataHeader.LockMetadata();
            }
            else
            {
                throw new DevirtualisationException(
                    "Failed to resolve runtime library. This could be a bug in the initial scanning phase. "
                    + "Try specifying the location of the runtime assembly in the devirtualizer options.");
            }

            return runtimeImage;
        }

        private void RunPipeline(DevirtualisationContext context)
        {
            foreach (var stage in Stages)
            {
                Logger.Log(Tag, $"Executing {stage.Name}...");
                stage.Run(context);
            }
        }

        private void RemoveFinalTraces(DevirtualisationOptions options, DevirtualisationContext context)
        {
            // Remove #koi stream.
            if (!context.AllVirtualisedMethodsRecompiled)
            {
                var header = context.TargetModule.Header;
                Logger.Debug(Tag, "Removing #Koi metadata stream...");
                header.StreamHeaders.Remove(header.GetStream<KoiStream>().StreamHeader);
            }
            else
            {
                Logger.Debug(Tag, "Not removing koi stream as some exports were ignored.");
            }
        }

        private void Rebuild(DevirtualisationOptions options, DevirtualisationContext context, bool rebuildRuntimeImage)
        {
            Logger.Log(Tag, $"Reassembling file...");
            context.TargetModule.Write(
                Path.Combine(options.OutputOptions.RootDirectory, Path.GetFileName(options.InputFile)));

            if (rebuildRuntimeImage)
            {
                context.RuntimeModule.Write(
                    Path.Combine(options.OutputOptions.RootDirectory, Path.GetFileName(context.Options.RuntimeFile)));
            }
        }
    }
}