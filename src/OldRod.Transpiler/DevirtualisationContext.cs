using System.Collections.Generic;
using AsmResolver.Net.Cts;
using OldRod.Core;
using OldRod.Core.Architecture;
using OldRod.Transpiler.Stages.OpCodeResolution;

namespace OldRod.Transpiler
{
    public class DevirtualisationContext
    {
        public DevirtualisationContext(MetadataImage targetImage, MetadataImage runtimeImage, ILogger logger)
        {
            TargetImage = targetImage;
            RuntimeImage = runtimeImage;
            Logger = logger;
        }

        public MetadataImage TargetImage
        {
            get;
        }

        public MetadataImage RuntimeImage
        {
            get;
        }

        public ILogger Logger
        {
            get;
        }

        public VMConstants Constants
        {
            get;
            set;
        }
        
        public OpCodeMapping OpCodeMapping
        {
            get;
            set;
        }

        public KoiStream KoiStream
        {
            get;
            set;
        }

        public IDictionary<long, ILInstruction> DisassembledInstructions
        {
            get;
            set;
        } = new Dictionary<long, ILInstruction>();
    }
}