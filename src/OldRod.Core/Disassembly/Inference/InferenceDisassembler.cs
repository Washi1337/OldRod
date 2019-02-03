using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver;
using AsmResolver.Net;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.DataFlow;
using OldRod.Core.Emulation;

namespace OldRod.Core.Disassembly.Inference
{
    public class InferenceDisassembler
    {
        private readonly MetadataImage _image;
        private readonly VMConstants _constants;
        private readonly KoiStream _koiStream;
        private readonly VCallProcessor _vCallProcessor;
        
        public InferenceDisassembler(MetadataImage image, VMConstants constants, KoiStream koiStream)
        {
            _image = image;
            _constants = constants;
            _koiStream = koiStream ?? throw new ArgumentNullException(nameof(koiStream));
            _vCallProcessor = new VCallProcessor(image, _constants, _koiStream);
        }
        
        public IDictionary<long, ILInstruction> Disassemble()
        {
            var instructions = new Dictionary<long, ILInstruction>();

            foreach (var export in _koiStream.Exports.Values)
                Disassemble(instructions, export);

            return instructions;
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
                    currentState.Registers[VMRegisters.IP] = new SymbolicValue(instruction);
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

            if (instruction.OpCode.Code == ILCode.VCALL)
            {
                // VCalls have embedded opcodes with different behaviours.
                nextStates.AddRange(_vCallProcessor.ProcessVCall(instruction, next));
            }
            else
            {
                // Push/pop necessary values from stack.
                int initial = next.Stack.Count;
                PopSymbolicValues(instruction, next);
                int popCount = initial - next.Stack.Count;

                initial = next.Stack.Count;
                PushSymbolicValues(instruction, next);
                int pushCount = next.Stack.Count - initial;

                // Apply control flow.
                PerformFlowControl(instruction, nextStates, next);

                if (instruction.InferredMetadata == null)
                    instruction.InferredMetadata = new InferredMetadata();
                
                instruction.InferredMetadata.InferredPopCount = popCount;
                instruction.InferredMetadata.InferredPushCount = pushCount;
            }

            return nextStates;
        }

        private void PopSymbolicValues(ILInstruction instruction, ProgramState next)
        {
            var arguments = new List<SymbolicValue>(2);
            switch (instruction.OpCode.StackBehaviourPop)
            {
                case ILStackBehaviour.None:
                    break;
                
                case ILStackBehaviour.PopRegister:
                case ILStackBehaviour.PopPtr:
                case ILStackBehaviour.PopByte:
                case ILStackBehaviour.PopWord:
                case ILStackBehaviour.PopDword:
                case ILStackBehaviour.PopQword:
                case ILStackBehaviour.PopReal32:
                case ILStackBehaviour.PopReal64:
                    var argument = next.Stack.Pop();
                    argument.Type = instruction.OpCode.StackBehaviourPop.GetArgumentType(0);
                    
                    // Check if instruction pops a value to a register.
                    if (instruction.OpCode.OperandType == ILOperandType.Register)
                        next.Registers[(VMRegisters) instruction.Operand] = new SymbolicValue(instruction);
                    
                    arguments.Add(argument);
                    break;
                
                case ILStackBehaviour.PopDword_PopDword:
                case ILStackBehaviour.PopQword_PopQword:
                case ILStackBehaviour.PopPtr_PopPtr:
                case ILStackBehaviour.PopPtr_PopObject:
                case ILStackBehaviour.PopPtr_PopByte:
                case ILStackBehaviour.PopPtr_PopWord:
                case ILStackBehaviour.PopPtr_PopDword:
                case ILStackBehaviour.PopPtr_PopQword:
                case ILStackBehaviour.PopObject_PopObject:
                case ILStackBehaviour.PopReal32_PopReal32:
                case ILStackBehaviour.PopReal64_PopReal64:
                    var argument2 = next.Stack.Pop();
                    var argument1 = next.Stack.Pop();

                    argument1.Type = instruction.OpCode.StackBehaviourPop.GetArgumentType(0);
                    argument2.Type = instruction.OpCode.StackBehaviourPop.GetArgumentType(1);
                    
                    arguments.Add(argument2);
                    arguments.Add(argument1);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Add instruction dependencies for data flow graph, in reverse order to negate natural stack behaviour.
            for (int i = arguments.Count - 1; i >= 0; i--)
                instruction.Dependencies.Add(arguments[i]);
        }

        private void PushSymbolicValues(ILInstruction instruction, ProgramState next)
        {
            switch (instruction.OpCode.StackBehaviourPush)
            {
                case ILStackBehaviour.None:
                    break;
                
                case ILStackBehaviour.PushPtr:
                case ILStackBehaviour.PushByte:
                case ILStackBehaviour.PushWord:
                case ILStackBehaviour.PushDword:
                case ILStackBehaviour.PushQword:
                case ILStackBehaviour.PushReal32:
                case ILStackBehaviour.PushReal64:
                case ILStackBehaviour.PushObject:
                case ILStackBehaviour.PushVar:
                    next.Stack.Push(new SymbolicValue(instruction)
                    {
                        Type = instruction.OpCode.StackBehaviourPush.GetResultType()
                    });
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void PerformFlowControl(ILInstruction instruction, List<ProgramState> nextStates, ProgramState next)
        {
            switch (instruction.OpCode.FlowControl)
            {
                case ILFlowControl.Next:
                {
                    // Normal flow.
                    nextStates.Add(next);
                    break;
                }
                case ILFlowControl.Call:
                case ILFlowControl.Jump:
                {
                    // Unconditional jump target.
                    var metadata = InferJumpTargets(instruction);
                    next.IP = metadata.InferredJumpTargets[0];
                    nextStates.Add(next);
                    break;
                }
                case ILFlowControl.ConditionalJump:
                {
                    // Next to normal jump target, we need to consider that either condition was false,
                    // or we returned from a call. Both have virtually the same effect on the flow analysis.
                    
                    var metadata = InferJumpTargets(instruction);
                    var branch = next.Copy();
                    branch.IP = metadata.InferredJumpTargets[0]; // TODO: handle switch statements.
                    nextStates.Add(branch);
                    nextStates.Add(next);
                    break;
                }
                case ILFlowControl.Return:
                {
                    // Return, do nothing.
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static JumpMetadata InferJumpTargets(ILInstruction instruction)
        {
            var emulator = new InstructionEmulator();
            emulator.EmulateDependentInstructions(instruction);
            
            // After partial emulation, IP is on stack.
            var nextIp = emulator.Stack.Pop();

            var metadata = new JumpMetadata(nextIp.U8);
            instruction.InferredMetadata = metadata;
            return metadata;
        }

     
    }
}