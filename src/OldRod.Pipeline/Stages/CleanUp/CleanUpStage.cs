using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;

namespace OldRod.Pipeline.Stages.CleanUp
{
    public class CleanUpStage : IStage
    {
        public const string Tag = "CleanUp";
        
        public string Name => "Clean up stage";

        public void Run(DevirtualisationContext context)
        {
            if (context.Options.IgnoredExports.Count > 0)
            {
                context.Logger.Debug(Tag, "Not cleaning up traces of KoiVM as some exports were ignored.");
            }
            else
            {
                context.Logger.Debug(Tag, "Cleaning up module constructor.");

                var cctor = context.TargetImage.GetModuleConstructor();

                // TODO: be more intelligent with removing the VM.init call.
                cctor.CilMethodBody.Instructions.Clear();
                cctor.CilMethodBody.Instructions.Add(CilInstruction.Create(CilOpCodes.Ret));
            }
        }
    }
}