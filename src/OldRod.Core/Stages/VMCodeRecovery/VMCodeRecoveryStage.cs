using System;
using AsmResolver;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly;

namespace OldRod.Core.Stages.VMCodeRecovery
{
    public class VMCodeRecoveryStage : IStage
    {
        public string Name => "VM code recovery stage";
        
        public void Run(DevirtualisationContext context)
        {
            foreach (var export in context.KoiStream.Exports)
            {
                Console.WriteLine(export.Key + ": " + export.Value.CodeOffset.ToString("X4") + ":");
                var disassembler = new LinearDisassembler(context.Constants,
                    new MemoryStreamReader(context.KoiStream.Data)
                    {
                        Position = export.Value.CodeOffset
                    }, export.Value.EntryKey);

                ILInstruction instruction;
                do
                {
                    instruction = disassembler.ReadNextInstruction();
                    Console.WriteLine(instruction);
                } while (instruction.OpCode.FlowControl == ILFlowControl.Next);

                Console.WriteLine();
            }

            Console.WriteLine("-");
            
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