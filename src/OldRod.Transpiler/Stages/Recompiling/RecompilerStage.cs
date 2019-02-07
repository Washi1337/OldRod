using System;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using OldRod.Core.Recompiler;

namespace OldRod.Transpiler.Stages.Recompiling
{
    public class RecompilerStage : IStage
    {
        public string Name => "CIL Recompiler stage";

        public void Run(DevirtualisationContext context)
        {
            var recompiler = new ILAstCompiler(context.TargetImage);

            var targetMethod = (MethodDefinition) context.TargetImage.ResolveMember(new MetadataToken(MetadataTokenType.Method, 3));
            var newBody = recompiler.Compile(targetMethod, context.CompilationUnits[context.KoiStream.Exports[3]]);
            targetMethod.CilMethodBody = newBody;
            
//            Console.WriteLine("Recompiled code:");
//            foreach (var instruction in newBody.Instructions) 
//                Console.WriteLine(instruction);
        }
    }
}