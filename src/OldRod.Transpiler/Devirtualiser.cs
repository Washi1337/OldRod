using System;
using System.Collections.Generic;
using System.IO;
using AsmResolver;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Emit;
using OldRod.Core;
using OldRod.Transpiler.Stages;
using OldRod.Transpiler.Stages.AstBuilding;
using OldRod.Transpiler.Stages.ConstantsResolution;
using OldRod.Transpiler.Stages.KoiStreamParsing;
using OldRod.Transpiler.Stages.OpCodeResolution;
using OldRod.Transpiler.Stages.Recompiling;
using OldRod.Transpiler.Stages.VMCodeRecovery;

namespace OldRod.Transpiler
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
                new OpCodeResolutionStage(),
                new VMCodeRecoveryStage(),
                new AstBuilderStage(),
                new RecompilerStage()
            };
        }
        
        public ILogger Logger { get; }
        
        public IList<IStage> Stages { get; }
        
        public void Devirtualise(string filePath, string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
            
            Logger.Log(Tag, "Started devirtualisation.");

            Logger.Log(Tag, $"Opening target file {filePath}");
            var assembly = WindowsAssembly.FromFile(filePath);
            var image = assembly.NetDirectory.MetadataHeader.LockMetadata();
            string directory = Path.GetDirectoryName(filePath);
            image.MetadataResolver = new DefaultMetadataResolver(new DefaultNetAssemblyResolver(directory));
            
            Logger.Log(Tag, "Resolving runtime library");
            // TODO: actually resolve from CIL (could be embedded).
            var runtimeAssembly = WindowsAssembly.FromFile(Path.Combine(directory, "Virtualization.dll"));
            var runtimeImage = runtimeAssembly.NetDirectory.MetadataHeader.LockMetadata();

            var context = new DevirtualisationContext(image, runtimeImage, Logger);

            foreach (var stage in Stages)
            {
                Logger.Log(Tag, $"Starting {stage.Name}");
                stage.Run(context);
            }

            Logger.Log(Tag, $"Commiting changes to metadata streams");
            image.Header.UnlockMetadata();
            
            Logger.Log(Tag, $"Reassembling file");
            assembly.Write(Path.Combine(outputDirectory, Path.GetFileName(filePath)), new CompactNetAssemblyBuilder(assembly));
            
            Logger.Log(Tag, $"Finished. All fish were caught and served!");
        }
        
        
    }
}