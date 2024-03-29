// Project OldRod - A KoiVM devirtualisation utility.
// Copyright (C) 2019 Washi
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using OldRod.Core.Architecture;

namespace OldRod.Core.Emulation
{
    // NOTE:
    // This emulator is more of a mini-emulator and is far from complete. As a result, the emulation might be very
    // inaccurate in lots of cases or result in an exception. Do not use for complete method emulation.
    
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
            // TODO: Perhaps include flag register updates?

            Registers[VMRegisters.IP] = new VMSlot() {U8 = (ulong) (instruction.Offset + instruction.Size)};

            if (instruction.ProgramState is null)
            {
                throw new EmulationException(
                    $"Failed to infer stack depth of {instruction} because the instruction does not have a program state assigned.");
            }

            Registers[VMRegisters.SP] = new VMSlot() {U4 = (uint) instruction.ProgramState.Stack.Count};
            
            switch (instruction.OpCode.Code)
            {
                case ILCode.PUSHR_OBJECT:
                {
                    // TODO: This is definitely not accurate, but works for the purpose of this mini emulator (pushr_object sp).
                    Stack.Push(new VMSlot
                    {
                        U8 = Registers[(VMRegisters) instruction.Operand].U8 
                    });
                    break;
                }
                case ILCode.PUSHR_BYTE:
                    Stack.Push(new VMSlot
                    {
                        U1 = Registers[(VMRegisters) instruction.Operand].U1
                    });
                    break;
              
                case ILCode.PUSHR_WORD:
                    Stack.Push(new VMSlot
                    {
                        U2 = Registers[(VMRegisters) instruction.Operand].U2
                    });
                    break;
                
                case ILCode.PUSHR_DWORD:
                    Stack.Push(new VMSlot
                    {
                        U4 = Registers[(VMRegisters) instruction.Operand].U4
                    });
                    break;
                
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
                
                case ILCode.PUSHI_QWORD:
                    Stack.Push(new VMSlot
                    {
                        U8 = Convert.ToUInt64(instruction.Operand)
                    });
                    break;

                case ILCode.ADD_DWORD:
                {
                    var op2 = Stack.Pop();
                    var op1 = Stack.Pop();
                    Stack.Push(new VMSlot
                    {
                        U4 = op1.U4 + op2.U4
                    });
                    break;
                }

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

                case ILCode.POP:
                {
                    Registers[(VMRegisters) instruction.Operand] = Stack.Pop();
                    break;
                }
                
                case ILCode.NOP:
                    break;
                
                default:
                    throw new EmulationException($"Failed to emulate the instruction {instruction}.",
                        new NotSupportedException($"OpCode {instruction.OpCode.Code} not supported yet!"));
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