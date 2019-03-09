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
using AsmResolver;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Emit;
using OldRod.Core;
using OldRod.Core.Architecture;
using OldRod.Pipeline.Stages;
using OldRod.Pipeline.Stages.AstBuilding;
using OldRod.Pipeline.Stages.CleanUp;
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

            bool rebuildRuntimeImage = options.RenameConstants;

            // Create output directory.
            options.OutputOptions.EnsureDirectoriesExist();

            var context = CreateDevirtualisationContext(options);

            // Run pipeline.
            RunPipeline(context);

            // Unlock images.
            Logger.Log(Tag, $"Commiting changes to metadata streams...");
            context.TargetImage.Header.UnlockMetadata();
            if (rebuildRuntimeImage)
                context.RuntimeImage.Header.UnlockMetadata();

            RemoveFinalTraces(options, context);

            // Rebuild.
            Rebuild(options, context, rebuildRuntimeImage);

            Logger.Log(Tag, $"Finished. All fish were caught and served!");
        }

        private DevirtualisationContext CreateDevirtualisationContext(DevirtualisationOptions options)
        {
            // Open target file.
            Logger.Log(Tag, $"Opening target file {options.InputFile}...");
            var assembly = WindowsAssembly.FromFile(options.InputFile);
            var header = assembly.NetDirectory.MetadataHeader;

            // Lock metadata and hook into md resolvers and md stream parsers.
            header.StreamParser = new KoiVmAwareStreamParser(options.KoiStreamName);
            var image = header.LockMetadata();
            string directory = Path.GetDirectoryName(options.InputFile);
            image.MetadataResolver = new DefaultMetadataResolver(new DefaultNetAssemblyResolver(directory));

            // Resolve runtime lib.
            Logger.Log(Tag, "Resolving runtime library...");
            // TODO: actually resolve from CIL (could be embedded).
            string runtimePath = Path.Combine(directory, "Virtualization.dll");
            var runtimeAssembly = WindowsAssembly.FromFile(runtimePath);
            var runtimeImage = runtimeAssembly.NetDirectory.MetadataHeader.LockMetadata();

            return new DevirtualisationContext(options, image, runtimeImage, Logger);

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
            if (options.IgnoredExports.Count == 0)
            {
                var header = context.TargetImage.Header;
                Logger.Debug(Tag, "Removing #Koi metadata stream.");
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
            context.TargetAssembly.Write(
                Path.Combine(options.OutputOptions.RootDirectory, Path.GetFileName(options.InputFile)),
                new CompactNetAssemblyBuilder(context.TargetAssembly));

            if (rebuildRuntimeImage)
            {
                context.RuntimeAssembly.Write(
                    Path.Combine(options.OutputOptions.RootDirectory, "Virtualisation.dll"),
                    new CompactNetAssemblyBuilder(context.RuntimeAssembly));
            }
        }
    }
}