using System;
using System.Collections.Generic;
using System.Linq;
using OldRod.Core.Architecture;

namespace OldRod.Core.Emulation
{
    public class InstructionEmulator
    {
        public InstructionEmulator()
        {
            for (int i = 0; i < (int) VMRegisters.Max; i++)
                Registers[(VMRegisters) i] = new VMSlot();
        }
        
        public IDictionary<VMRegisters, VMSlot> Registers
        {
            get;
        } = new Dictionary<VMRegisters, VMSlot>();
        
        public Stack<VMSlot> Stack
        {
            get;
        } = new Stack<VMSlot>();

        public void EmulateInstruction(ILInstruction instruction)
        {
            Registers[VMRegisters.IP] = new VMSlot() {U8 = (ulong) (instruction.Offset + instruction.Size)};
            
            switch (instruction.OpCode.Code)
            {
                case ILCode.PUSHR_QWORD:
                    Stack.Push(new VMSlot
                    {
                        U8 = Registers[(VMRegisters) instruction.Operand].U8
                    });
                    break;
                
                case ILCode.PUSHI_DWORD:
                    uint imm = Convert.ToUInt32(instruction.Operand);
                    ulong sx = (imm & 0x80000000) != 0 ? 0xffffffffUL << 32 : 0;
                    Stack.Push(new VMSlot
                    {
                        U8 = sx | imm
                    });
                    break;

                case ILCode.ADD_QWORD:
                {
                    var op2 = Stack.Pop();
                    var op1 = Stack.Pop();
                    Stack.Push(new VMSlot
                    {
                        U8 = op1.U8 + op2.U8
                    });
                    break;
                }
                
                default:
                    throw new NotSupportedException();
            }
        }
        
        public void EmulateDependentInstructions(ILInstruction instruction)
        {
            // TODO: Use data flow graph instead to determine order of instructions.
            var queue = instruction.GetAllDependencies()
                .OrderBy(x => x.Offset)
                .ToList();

            foreach (var source in queue)
                EmulateInstruction(source);
        }
    }
}