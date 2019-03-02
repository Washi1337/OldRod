using OldRod.Core.Architecture;

namespace OldRod.Pipeline.Stages.KoiStreamParsing
{
    public class KoiStreamParserStage : IStage
    {
        public const string Tag = "#KoiParser";
        
        public string Name => "#Koi stream parsing stage";
        
        public void Run(DevirtualisationContext context)
        {
            context.Logger.Debug(Tag, "Parsing #Koi stream...");
            context.KoiStream = context.TargetImage.Header.GetStream<KoiStream>();
            
            if (context.KoiStream == null)
                throw new DevirtualisationException("Koi stream was not found in PE.");

            foreach (uint ignored in context.Options.IgnoredExports)
            {
                context.Logger.Debug(Tag, "Ignoring export "  + ignored + " as per user-defined parameters.");
                context.KoiStream.Exports.Remove(ignored);
            }
        }
        
    }
}