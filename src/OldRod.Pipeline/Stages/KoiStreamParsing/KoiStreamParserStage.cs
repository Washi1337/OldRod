using AsmResolver;
using AsmResolver.Net;
using OldRod.Core.Architecture;

namespace OldRod.Pipeline.Stages.KoiStreamParsing
{
    public class KoiStreamParserStage : IStage
    {
        public const string Tag = "#KoiParser";
        
        public string Name => "#Koi stream parsing stage";
        
        public void Run(DevirtualisationContext context)
        {
            context.Logger.Debug(Tag, "Locating #Koi stream...");
            var dataStream = (CustomMetadataStream) context.TargetImage.Header.GetStream(context.Options.KoiStreamName);

            context.Logger.Debug(Tag, "Parsing #Koi stream...");
            context.KoiStream = KoiStream.FromBytes(dataStream.Data);
            context.KoiStream.StartOffset = dataStream.StartOffset;

            foreach (uint ignored in context.Options.IgnoredExports)
            {
                context.Logger.Debug(Tag, "Ignoring export "  + ignored + " as per user-defined parameters.");
                context.KoiStream.Exports.Remove(ignored);
            }
        }
        
    }
}