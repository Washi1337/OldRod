using System;
using System.IO;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using OldRod.Core.CodeGen;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Recompiler;
using Rivers.Serialization.Dot;

namespace OldRod.Pipeline.Stages.Recompiling
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

            if (context.Options.DumpControlFlowGraphs)
            {
                targetMethod.CilMethodBody.Instructions.CalculateOffsets();
                
                using (var fs = File.CreateText(Path.Combine(context.Options.OutputDirectory, "export3_cilast.dot")))
                {
                    var writer = new DotWriter(fs, new BasicBlockSerializer(targetMethod.CilMethodBody));
                    writer.Write(Utilities.ConvertToGraphViz(cilUnit.ControlFlowGraph, CilAstBlock.AstBlockProperty));
                }
            }
//            Console.WriteLine("Recompiled code:");
//            foreach (var instruction in newBody.Instructions) 
//                Console.WriteLine(instruction);
        }
    }
}