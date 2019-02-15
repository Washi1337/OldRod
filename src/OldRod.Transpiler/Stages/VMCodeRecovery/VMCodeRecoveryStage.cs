using System;
using System.Linq;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.Inference;

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

            foreach (var entry in flowGraphs)
            {
                Console.WriteLine(entry.Key.CodeOffset.ToString("X4"));
                foreach (var node in entry.Value.Nodes.OrderBy(x => x.Name))
                {
                    var block = (ILBasicBlock) node.UserData[ILBasicBlock.BasicBlockProperty];
                    foreach (var instruction in block.Instructions)
                    {
                        Console.WriteLine("{0,-50} {1, -50} {2}", 
                            instruction.ToString(), 
                            instruction.ProgramState.Stack.ToString(),
//                            instruction.ProgramState.Registers.ToString(),
                            instruction.InferredMetadata);
                    }
                }

                Console.WriteLine();
            }

            context.ControlFlowGraphs = flowGraphs;
        }
    }
}