using System;
using AsmResolver;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Stages.VMCodeRecovery
{
    public class VMCodeRecoveryStage : IStage
    {
        public string Name => "VM code recovery stage";
        
        public void Run(DevirtualisationContext context)
        {            
            var infDis = new InferenceDisassembler(context.TargetImage, context.Constants, context.KoiStream);
            foreach (var instruction in infDis.Disassemble())
            {
                Console.Write(instruction.ToString().PadRight(40));
                Console.Write(instruction.ProgramState.Stack.ToString().PadRight(25));
//                Console.WriteLine(instruction.ProgramState.Registers);
                Console.WriteLine(instruction.InferredMetadata);
            }
        }
    }
}