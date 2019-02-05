using System;
using System.Linq;
using AsmResolver;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.Inference;
using Rivers.Serialization.Dot;

namespace OldRod.Transpiler.Stages.VMCodeRecovery
{
    public class VMCodeRecoveryStage : IStage
    {
        public const string Tag = "VMCode";
        
        public string Name => "VM code recovery stage";
        
        public void Run(DevirtualisationContext context)
        {            
            var infDis = new InferenceDisassembler(context.TargetImage, context.Constants, context.KoiStream);
            infDis.Logger = context.Logger;
            
            context.Logger.Debug(Tag, "Disassembling #Koi stream...");
            var flowGraphs = infDis.BuildFlowGraphs();

//            var writer = new DotWriter(Console.Out, new BasicBlockSerializer());
//            foreach (var entry in flowGraphs)
//            {
//                Console.WriteLine(entry.Key.CodeOffset.ToString("X4"));
//                writer.Write(entry.Value);
//                Console.WriteLine();
//            }

            context.ControlFlowGraphs = flowGraphs;
        }
    }
}