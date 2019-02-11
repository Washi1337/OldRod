using System;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using OldRod.Core.Assembly;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Recompiler;

namespace OldRod.Transpiler.Stages.Recompiling
{
    public class RecompilerStage : IStage
    {
        public string Name => "CIL Recompiler stage";

        public void Run(DevirtualisationContext context)
        {
            var recompiler = new ILToCilRecompiler(context.TargetImage);
            var targetMethod = (MethodDefinition) context.TargetImage.ResolveMember(new MetadataToken(MetadataTokenType.Method, 3));
            var cilUnit = (CilCompilationUnit) recompiler.VisitCompilationUnit(context.CompilationUnits[context.KoiStream.Exports[3]]);
            
            var generator = new CilMethodBodyGenerator(context.TargetImage, context.Constants);
            targetMethod.CilMethodBody = generator.Compile(targetMethod, cilUnit);

//            Console.WriteLine("Recompiled code:");
//            foreach (var instruction in newBody.Instructions) 
//                Console.WriteLine(instruction);
        }
    }
}