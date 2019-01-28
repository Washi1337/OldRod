using AsmResolver;
using AsmResolver.Net;
using Carp.Core.Architecture;

namespace Carp.Core.Stages.KoiStreamParsing
{
    public class KoiStreamParser : IStage
    {
        public const string Tag = "#KoiParser";
        
        public string Name => "#Koi stream parser";
        
        public void Run(DevirtualisationContext context)
        {
            context.Logger.Log(Tag, "Locating #Koi stream...");
            var dataStream = (CustomMetadataStream) context.TargetImage.Header.GetStream("#Koi") ;

            context.Logger.Log(Tag, "Parsing #Koi stream...");
            context.KoiStream = KoiStream.FromReader(new MemoryStreamReader(dataStream.Data));
        }
        
    }
}