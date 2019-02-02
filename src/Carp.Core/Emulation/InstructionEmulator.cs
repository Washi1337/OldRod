using System;
using System.Collections.Generic;
using Carp.Core.Architecture;

namespace Carp.Core.Emulation
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
                /*
                 * IL_008A: PUSHR_QWORD IP                 {?}                 {R0: ?, R1: ?, R2: ?, R3: ?, R4: ?, R5: ?, R6: ?, R7: ?, BP: 0079, SP: 0087, IP: 008A, FL: ?, K1: ?, K2: ?, M1: ?, M2: ?}
                   IL_008D: PUSHI_DWORD 26                 {?, 008A}           {R0: ?, R1: ?, R2: ?, R3: ?, R4: ?, R5: ?, R6: ?, R7: ?, BP: 0079, SP: 0087, IP: 008D, FL: ?, K1: ?, K2: ?, M1: ?, M2: ?}
                   IL_0093: ADD_QWORD                      {?, 008A, 008D}     {R0: ?, R1: ?, R2: ?, R3: ?, R4: ?, R5: ?, R6: ?, R7: ?, BP: 0079, SP: 0087, IP: 0093, FL: ?, K1: ?, K2: ?, M1: ?, M2: ?}
                   IL_0095: JMP                            {?, 0093}           {R0: ?, R1: ?, R2: ?, R3: ?, R4: ?, R5: ?, R6: ?, R7: ?, BP: 0079, SP: 0087, IP: 0095, FL: ?, K1: ?, K2: ?, M1: ?, M2: ?}
                 */
                
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
    }
}