using AsmResolver.Net.Cts;
using Carp.Core.Architecture;
using Carp.Core.Stages.OpCodeResolution;

namespace Carp.Core
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