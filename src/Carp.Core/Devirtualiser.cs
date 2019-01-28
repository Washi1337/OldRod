using System;
using System.Collections.Generic;
using System.IO;
using AsmResolver;
using AsmResolver.Net.Cts;
using Carp.Core.Stages;
using Carp.Core.Stages.ConstantsResolution;
using Carp.Core.Stages.KoiStreamParsing;
using Carp.Core.Stages.OpCodeResolution;
using Carp.Core.Stages.Transpiler;
using Carp.Core.Stages.VMCodeRecovery;

namespace Carp.Core
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
                new TranspilerStage()
            };
        }
        
        public ILogger Logger { get; }
        
        public IList<IStage> Stages { get; }
        
        public void Devirtualise(string filePath)
        {
            Logger.Log(Tag, "Started devirtualisation.");

            Logger.Log(Tag, "Opening target file...");
            var assembly = WindowsAssembly.FromFile(filePath);
            var image = assembly.NetDirectory.MetadataHeader.LockMetadata();
            string directory = Path.GetDirectoryName(filePath);
            image.MetadataResolver = new DefaultMetadataResolver(new DefaultNetAssemblyResolver(directory));
            
            Logger.Log(Tag, "Resolving runtime library...");
            // TODO: actually resolve from CIL (could be embedded).
            var runtimeAssembly = WindowsAssembly.FromFile(Path.Combine(directory, "Virtualization.dll"));
            var runtimeImage = runtimeAssembly.NetDirectory.MetadataHeader.LockMetadata();

            var context = new DevirtualisationContext(image, runtimeImage, Logger);

            foreach (var stage in Stages)
            {
                Logger.Log(Tag, $"Starting {stage.Name}");
                stage.Run(context);
            }
        }
        
        
    }
}