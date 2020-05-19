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
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Metadata;
using AsmResolver.PE.File;
using OldRod.Core;
using OldRod.Core.Architecture;
using OldRod.Pipeline.Stages;
using OldRod.Pipeline.Stages.AstBuilding;
using OldRod.Pipeline.Stages.CleanUp;
using OldRod.Pipeline.Stages.CodeAnalysis;
using OldRod.Pipeline.Stages.ConstantsResolution;
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

            // Rebuild.
            Rebuild(options, context);

            Logger.Log(Tag, $"Finished. All fish were caught and served!");
        }

        private DevirtualisationContext CreateDevirtualisationContext(DevirtualisationOptions options)
        {
            string workingDirectory = Path.GetDirectoryName(options.InputFile);
            
            // Open target PE.
            Logger.Log(Tag, $"Opening target file {options.InputFile}...");
            var peFile = PEFile.FromFile(options.InputFile);
            
            // Create #Koi stream aware metadata reader.
            var streamReader = options.OverrideKoiStreamData
                ? new DefaultMetadataStreamReader(peFile)
                : (IMetadataStreamReader) new KoiVmAwareStreamReader(peFile, options.KoiStreamName, Logger);

            var peImage = PEImage.FromFile(peFile, new PEReadParameters(peFile)
            {
                MetadataStreamReader = streamReader
            });

            var metadata = peImage.DotNetDirectory?.Metadata;
            if (metadata is null)
                throw new BadImageFormatException("Assembly does not contain a valid .NET header.");

            // If custom koi stream data was provided, inject it.
            KoiStream koiStream;
            if (!options.OverrideKoiStreamData)
            {
                koiStream = metadata.GetStream<KoiStream>() ?? throw new DevirtualisationException(
                    "Koi stream was not found in the target PE. This could be because the input file is " +
                    "not protected with KoiVM, or the metadata stream uses a name that is different " +
                    "from the one specified in the input parameters.");
            }
            else
            {
                string path = Path.IsPathRooted(options.KoiStreamDataFile)
                    ? options.KoiStreamDataFile
                    : Path.Combine(workingDirectory, options.KoiStreamDataFile);

                Logger.Log(Tag, $"Opening external Koi stream data file {path}...");
                var contents = File.ReadAllBytes(path);

                // Replace original koi stream if it existed.
                koiStream = new KoiStream(options.KoiStreamName, new DataSegment(contents), Logger);
            }

            // Ignore invalid / encrypted method bodies when specified.
            var moduleReadParameters = new ModuleReadParameters(workingDirectory)
            {
                MethodBodyReader = new DefaultMethodBodyReader
                {
                    ThrowOnInvalidMethodBody = !options.IgnoreInvalidMethodBodies
                }
            };

            var module = ModuleDefinition.FromImage(peImage, moduleReadParameters);
            var runtimeModule = ResolveRuntimeModule(options, module);

            koiStream.ResolutionContext = module;
            return new DevirtualisationContext(options, module, runtimeModule, koiStream, Logger);
        }

        private ModuleDefinition ResolveRuntimeModule(DevirtualisationOptions options, ModuleDefinition targetModule)
        {
            ModuleDefinition runtimeModule = null;

            if (options.AutoDetectRuntimeFile)
            {
                Logger.Debug(Tag, "Attempting to autodetect location of the runtime library...");
                var runtimeAssemblies = targetModule.AssemblyReferences
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
                runtimeModule = targetModule;
            }
            else if (options.RuntimeFile != null)
            {
                // Resolve runtime library.
                Logger.Log(Tag, $"Opening external runtime library located at {options.RuntimeFile}...");

                string runtimePath = Path.IsPathRooted(options.RuntimeFile)
                    ? options.RuntimeFile
                    : Path.Combine(Path.GetDirectoryName(options.InputFile), options.RuntimeFile);
                runtimeModule = ModuleDefinition.FromFile(runtimePath);
            }
            else
            {
                throw new DevirtualisationException(
                    "Failed to resolve runtime library. This could be a bug in the initial scanning phase. "
                    + "Try specifying the location of the runtime assembly in the devirtualizer options.");
            }

            return runtimeModule;
        }

        private void RunPipeline(DevirtualisationContext context)
        {
            foreach (var stage in Stages)
            {
                Logger.Log(Tag, $"Executing {stage.Name}...");
                stage.Run(context);
            }
        }

        private void Rebuild(DevirtualisationOptions options, DevirtualisationContext context)
        {
            bool rebuildRuntimeImage = options.RenameSymbols && !options.RuntimeIsEmbedded;
            
            Logger.Log(Tag, $"Reassembling image...");
            context.TargetModule.Write(
                Path.Combine(options.OutputOptions.RootDirectory, Path.GetFileName(options.InputFile)));

            if (rebuildRuntimeImage)
            {
                Logger.Log(Tag, $"Reassembling runtime image...");
                context.RuntimeModule.Write(
                    Path.Combine(options.OutputOptions.RootDirectory, Path.GetFileName(context.Options.RuntimeFile)));
            }
        }
    }
}