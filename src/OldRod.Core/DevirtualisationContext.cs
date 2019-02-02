using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;
using OldRod.Core.Stages.OpCodeResolution;

namespace OldRod.Core
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
    }
}