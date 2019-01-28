using System;
using Carp.Core.Disassembly;

namespace Carp.Core.Stages.VMCodeRecovery
{
    public class VMCodeRecoveryStage : IStage
    {
        public string Name => "VM code recovery stage";
        
        public void Run(DevirtualisationContext context)
        {
            var infDis = new InferenceDisassembler(context.Constants, context.KoiStream);
            foreach (var instruction in infDis.Disassemble())
            {
                Console.Write(instruction.ToString().PadRight(40));
                Console.Write(instruction.ProgramState.Stack.ToString().PadRight(20));
                Console.WriteLine(instruction.ProgramState.Registers);
            }
        }
    }
}