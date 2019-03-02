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
using OldRod.Pipeline.Stages.VMEntryDetection;

namespace OldRod.Pipeline
{
    public class Devirtualiser
    {
        public const string Tag = "Main";
        
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
        
        public ILogger Logger { get; }
        
        public IList<IStage> Stages { get; }
        
        public void Devirtualise(DevirtualisationOptions options)
        {
            Logger.Log(Tag, "Started devirtualisation.");
            if (!Directory.Exists(options.OutputDirectory))
                Directory.CreateDirectory(options.OutputDirectory);

            Logger.Log(Tag, $"Opening target file {options.InputFile}...");
            var assembly = WindowsAssembly.FromFile(options.InputFile);
            var header = assembly.NetDirectory.MetadataHeader;
            header.StreamParser = new KoiVmAwareStreamParser(options.KoiStreamName);
            
            var image = header.LockMetadata();
            string directory = Path.GetDirectoryName(options.InputFile);
            image.MetadataResolver = new DefaultMetadataResolver(new DefaultNetAssemblyResolver(directory));
            
            Logger.Log(Tag, "Resolving runtime library...");
            // TODO: actually resolve from CIL (could be embedded).
            var runtimeAssembly = WindowsAssembly.FromFile(Path.Combine(directory, "Virtualization.dll"));
            var runtimeImage = runtimeAssembly.NetDirectory.MetadataHeader.LockMetadata();

            RunPipeline(options, image, runtimeImage);

            Logger.Log(Tag, $"Commiting changes to metadata streams...");
            image.Header.UnlockMetadata();

            if (options.IgnoredExports.Count == 0)
            {
                Logger.Debug(Tag, "Removing #Koi metadata stream.");
                header.StreamHeaders.Remove(header.GetStream<KoiStream>().StreamHeader);
            }
            else
            {
                Logger.Debug(Tag, "Not removing koi stream as some exports were ignored.");
            }
            
            Logger.Log(Tag, $"Reassembling file...");
            assembly.Write(
                Path.Combine(options.OutputDirectory, Path.GetFileName(options.InputFile)), 
                new CompactNetAssemblyBuilder(assembly));
            
            Logger.Log(Tag, $"Finished. All fish were caught and served!");
        }

        private void RunPipeline(DevirtualisationOptions options, MetadataImage image, MetadataImage runtimeImage)
        {
            var context = new DevirtualisationContext(options, image, runtimeImage, Logger);

            foreach (var stage in Stages)
            {
                Logger.Log(Tag, $"Executing {stage.Name}...");
                stage.Run(context);
            }
        }
    }
}