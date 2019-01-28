using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver;
using Carp.Core.Architecture;

namespace Carp.Core.Disassembly
{
    public class InferenceDisassembler
    {
        private readonly VMConstants _constants;
        private readonly KoiStream _koiStream;
        
        public InferenceDisassembler(VMConstants constants, KoiStream koiStream)
        {
            _constants = constants;
            _koiStream = koiStream ?? throw new ArgumentNullException(nameof(koiStream));
        }
        
        public IList<ILInstruction> Disassemble()
        {
            var instructions = new Dictionary<long, ILInstruction>();

            foreach (var export in _koiStream.Exports)
                Disassemble(instructions, export.Value);

            return instructions.OrderBy(x => x.Key).Select(x => x.Value).ToList();
        }

        private void Disassemble(IDictionary<long, ILInstruction> visited, VMExportInfo exportInfo)
        {
            var disassembler = new LinearDisassembler(_constants, new MemoryStreamReader(_koiStream.Data)
            {
                Position = exportInfo.CodeOffset
            }, exportInfo.EntryKey);

            var initialState = new ProgramState() {IP = exportInfo.CodeOffset};
            
            var agenda = new Stack<ProgramState>();
            agenda.Push(initialState);

            while (agenda.Count > 0)
            {
                var currentState = agenda.Pop();
                
                // Check if offset is already visited before.
                if (visited.TryGetValue((long) currentState.IP, out var instruction))
                {
                    // Check if program state is changed, if so, we need to revisit.
                    if (instruction.ProgramState.MergeWith(currentState))
                        currentState = instruction.ProgramState;
                    else
                        continue;
                }
                else
                {
                    // Offset is not visited yet, read instruction. 
                    disassembler.Reader.Position = (long) currentState.IP;
                    instruction = disassembler.ReadNextInstruction();
                    instruction.ProgramState = currentState;
                    currentState.Registers[VMRegisters.IP] = new ValueReference(instruction);
                    visited.Add((long) currentState.IP, instruction);
                }

                // Determine next states.
                foreach (var state in GetNextStates(currentState, instruction))
                    agenda.Push(state);
            }
        }

        private IList<ProgramState> GetNextStates(ProgramState currentState, ILInstruction instruction)
        {
            var nextStates = new List<ProgramState>(1);
            var next = currentState.Copy();
            next.IP += (ulong) instruction.Size;

            switch (instruction.OpCode.Code)
            {
                // Push reg:
                case ILCode.PUSHR_OBJECT:
                case ILCode.PUSHR_BYTE:
                case ILCode.PUSHR_WORD:
                case ILCode.PUSHR_DWORD:
                case ILCode.PUSHR_QWORD:
                    next.Stack.Push(next.Registers[(VMRegisters) instruction.Operand]);
                    nextStates.Add(next);
                    break;
                
                // Push constant:
                case ILCode.PUSHI_DWORD:
                case ILCode.PUSHI_QWORD:
                    next.Stack.Push(new ValueReference(instruction));
                    nextStates.Add(next);
                    break;
                
                // Pop to reg:
                case ILCode.POP:
                    next.Stack.Pop();
                    next.Registers[(VMRegisters) instruction.Operand] = new ValueReference(instruction);
                    nextStates.Add(next);
                    break;
                
                // Unary operators:
                case ILCode.LIND_PTR:
                case ILCode.LIND_OBJECT:
                case ILCode.LIND_BYTE:
                case ILCode.LIND_WORD:
                case ILCode.LIND_DWORD:
                case ILCode.LIND_QWORD:
                case ILCode.SX_BYTE:
                case ILCode.SX_WORD:
                case ILCode.SX_DWORD:
                case ILCode.FCONV_R32_R64:
                case ILCode.FCONV_R64_R32:
                case ILCode.FCONV_R32:
                case ILCode.FCONV_R64:
                case ILCode.ICONV_PTR:
                case ILCode.ICONV_R64:
                    next.Stack.Pop();
                    next.Stack.Push(new ValueReference(instruction));
                    nextStates.Add(next);
                    break;
                
                // Binary operators:
                case ILCode.ADD_DWORD:
                case ILCode.ADD_QWORD:
                case ILCode.ADD_R32:
                case ILCode.ADD_R64:
                case ILCode.SUB_R32:
                case ILCode.SUB_R64:
                case ILCode.MUL_DWORD:
                case ILCode.MUL_QWORD:
                case ILCode.MUL_R32:
                case ILCode.MUL_R64:
                case ILCode.DIV_DWORD:
                case ILCode.DIV_QWORD:
                case ILCode.DIV_R32:
                case ILCode.DIV_R64:
                case ILCode.REM_DWORD:
                case ILCode.REM_QWORD:
                case ILCode.REM_R32:
                case ILCode.REM_R64:
                case ILCode.SHR_DWORD:
                case ILCode.SHR_QWORD:
                case ILCode.SHL_DWORD:
                case ILCode.SHL_QWORD:
                case ILCode.SIND_PTR:
                case ILCode.SIND_OBJECT:
                case ILCode.SIND_BYTE:
                case ILCode.SIND_WORD:
                case ILCode.SIND_DWORD:
                case ILCode.SIND_QWORD:
                case ILCode.NOR_DWORD:
                case ILCode.NOR_QWORD:
                case ILCode.CMP:
                case ILCode.CMP_DWORD:
                case ILCode.CMP_QWORD:
                case ILCode.CMP_R32:
                case ILCode.CMP_R64:
                    next.Stack.Pop();
                    next.Stack.Pop();
                    next.Stack.Push(new ValueReference(instruction));
                    nextStates.Add(next);
                    break;

                // Unconditional jump:
                case ILCode.JMP:
                {
                    var nextIp = next.Stack.Pop();
                    // TODO: infer jump targets.
                    break;
                }

                // Conditional jump:
                case ILCode.JZ:
                case ILCode.JNZ:
                case ILCode.SWT:
                {
                    var nextIp = next.Stack.Pop();
                    var value = next.Stack.Pop();
                    // TODO: infer jump targets.

                    nextStates.Add(next);
                    break;
                }

                // Return:
                case ILCode.RET:
                    break;

                // Calls:
                case ILCode.CALL:
                {
                    var nextIp = next.Stack.Pop();
                    // TODO: infer call target.
                    
                    nextStates.Add(next);
                    break;
                }
                
                // Not supported yet:
                case ILCode.VCALL:
                case ILCode.TRY:
                case ILCode.LEAVE:
                    break;
                
                case ILCode.NOP:
                default:
                    nextStates.Add(next);
                    break;
            }

            return nextStates;
        }
    }
}