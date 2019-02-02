using System;
using System.Linq;
using AsmResolver;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly;
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
            
            context.Logger.Debug(Tag, "Disassembling #Koi stream...");
            var disassembly = infDis.Disassemble();

            foreach (var instruction in disassembly.OrderBy(x => x.Key).Select(x => x.Value))
            {
                Console.Write(instruction.ToString().PadRight(40));
                Console.Write(instruction.ProgramState.Stack.ToString().PadRight(50));
                Console.Write(instruction.InferredMetadata.ToString().PadRight(30));

                foreach (var entry in context.KoiStream.Exports)
                {
                    if (entry.Value.CodeOffset == instruction.Offset)
                    {
                        Console.Write("Export " + entry.Key);
                        break;
                    }
                }

                Console.WriteLine();
            }

            context.DisassembledInstructions = disassembly;
        }
    }
}