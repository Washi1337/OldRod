using System;
using System.Linq;
using AsmResolver;
using AsmResolver.Net.Cil;
using Carp.Core.Architecture;

namespace Carp.Core.Stages.Transpiler
{
    public class TranspilerStage : IStage
    {
        public const string Tag = "Transpiler";
        
        public string Name => "KoiVM IL Transpiler";

        public void Run(DevirtualisationContext context)
        {
            foreach (var export in context.KoiStream.Exports.Values)
            {
                Console.WriteLine(export.CodeOffset.ToString("X8"));
                var disassembler = new Disassembler(context.Constants,
                    new MemoryStreamReader(context.KoiStream.Data)
                    {
                        Position = export.CodeOffset
                    },
                    export.EntryKey);

                ILInstruction instruction;
                do
                {
                    instruction = disassembler.ReadNextInstruction();
                    Console.WriteLine(instruction);
                } while (instruction.OpCode.FlowControl == ILFlowControl.Next);

                Console.WriteLine();
            }
        }
    }
}