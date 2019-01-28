using AsmResolver;
using AsmResolver.Net;
using Carp.Core.Architecture;

namespace Carp.Core.Stages.KoiStreamParsing
{
    public class KoiStreamParserStage : IStage
    {
        public const string Tag = "#KoiParser";
        
        public string Name => "#Koi stream parsing stage";
        
        public void Run(DevirtualisationContext context)
        {
            context.Logger.Debug(Tag, "Locating #Koi stream...");
            var dataStream = (CustomMetadataStream) context.TargetImage.Header.GetStream("#Koi") ;

            context.Logger.Debug(Tag, "Parsing #Koi stream...");
            context.KoiStream = KoiStream.FromBytes(dataStream.Data);
            context.KoiStream.StartOffset = dataStream.StartOffset;
        }
        
    }
}