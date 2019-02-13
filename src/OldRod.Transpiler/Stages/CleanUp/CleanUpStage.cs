using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;

namespace OldRod.Transpiler.Stages.CleanUp
{
    public class CleanUpStage : IStage
    {
        public const string Tag = "CleanUp";
        
        public string Name => "Clean up stage";

        public void Run(DevirtualisationContext context)
        {
            context.Logger.Debug(Tag, "Cleaning up module constructor.");
            
            var cctor = (MethodDefinition) context.TargetImage.ResolveMember(
                new MetadataToken(MetadataTokenType.Method,
                1));
            
            // TODO: be more intelligent with removing the VM.init call.
            cctor.CilMethodBody.Instructions.Clear();
            cctor.CilMethodBody.Instructions.Add(CilInstruction.Create(CilOpCodes.Ret));
        }
    }
}