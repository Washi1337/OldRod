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
using AsmResolver;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.DataFlow;
using OldRod.Core.Emulation;

namespace OldRod.Core.Disassembly.Inference
{
    public class InferenceDisassembler
    {
        private const string Tag = "InferenceDisasm";
        
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

        public ILogger Logger
        {
            get;
            set;
        } = EmptyLogger.Instance;
        
        public IDictionary<VMExportInfo, ControlFlowGraph> BuildFlowGraphs()
        {
            // TODO: maybe reuse instructions and blockHeaders dictionary for speed up?
            
            var result = new Dictionary<VMExportInfo, ControlFlowGraph>();
            foreach (var entry in _koiStream.Exports)
            {
                var export = entry.Value;
                var instructions = new Dictionary<long, ILInstruction>();
                var blockHeaders = new HashSet<long>();
                
                // Raw disassemble.
                Logger.Debug(Tag, $"Disassembling export {entry.Key}...");
                Disassemble(instructions, blockHeaders, export);
                
                // Construct flow graph.
                Logger.Debug(Tag, $"Building CFG for export {entry.Key}...");
                var graph = ControlFlowGraphBuilder.BuildGraph(export, instructions.Values, blockHeaders);
                result.Add(export, graph);
            }

            return result;
        }

        private void Disassemble(IDictionary<long, ILInstruction> visited, ISet<long> blockHeaders, VMExportInfo exportInfo)
        {
            var decoder = new InstructionDecoder(_constants, new MemoryStreamReader(_koiStream.Data)
            {
                Position = exportInfo.CodeOffset
            }, exportInfo.EntryKey);

            var initialState = new ProgramState()
            {
                IP = exportInfo.CodeOffset,
                Key = exportInfo.EntryKey
            };
            
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
                    decoder.Reader.Position = (long) currentState.IP;
                    decoder.CurrentKey = currentState.Key;
                    
                    instruction = decoder.ReadNextInstruction();
                    
                    instruction.ProgramState = currentState;
                    visited.Add((long) currentState.IP, instruction);
                }

                currentState.Registers[VMRegisters.IP] = new SymbolicValue(instruction, VMType.Qword);
                
                // Determine next states.
                foreach (var state in GetNextStates(blockHeaders, currentState, instruction))
                {
                    state.Key = decoder.CurrentKey;
                    agenda.Push(state);
                }
            }
        }

        private IList<ProgramState> GetNextStates(
            ISet<long> blockHeaders, 
            ProgramState currentState, 
            ILInstruction instruction)
        {
            var nextStates = new List<ProgramState>(1);
            var next = currentState.Copy();
            next.IP += (ulong) instruction.Size;

            if (instruction.OpCode.AffectsFlags)
                next.Registers[VMRegisters.FL] = new SymbolicValue(instruction, VMType.Byte);

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
                PerformFlowControl(blockHeaders, instruction, nextStates, next);

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
                
                case ILStackBehaviour.PopAny:
                case ILStackBehaviour.PopPtr:
                case ILStackBehaviour.PopByte:
                case ILStackBehaviour.PopWord:
                case ILStackBehaviour.PopDword:
                case ILStackBehaviour.PopQword:
                case ILStackBehaviour.PopReal32:
                case ILStackBehaviour.PopReal64:
                    var argument = next.Stack.Pop();
                    
                    if (instruction.OpCode.StackBehaviourPop != ILStackBehaviour.PopAny)
                        argument.Type = instruction.OpCode.StackBehaviourPop.GetArgumentType(0);
                    
                    // Check if instruction pops a value to a register.
                    if (instruction.OpCode.OperandType == ILOperandType.Register)
                        next.Registers[(VMRegisters) instruction.Operand] = new SymbolicValue(instruction, argument.Type);
                    
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
            for (int i = arguments.Count - 1, j = 0; i >= 0; i--, j++)
                instruction.Dependencies.AddOrMerge(j, arguments[i]);
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
                    next.Stack.Push(new SymbolicValue(
                        instruction,
                        instruction.OpCode.StackBehaviourPush.GetResultType())
                    );
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void PerformFlowControl(ISet<long> blockHeaders, ILInstruction instruction, List<ProgramState> nextStates, ProgramState next)
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
                    blockHeaders.Add((long) next.IP);
                    
                    // Unconditional jump target.
                    var metadata = InferJumpTargets(instruction);
                    if (metadata != null)
                    {
                        next.IP = metadata.InferredJumpTargets[0];
                        blockHeaders.Add((long) next.IP);
                        nextStates.Add(next);
                    }

                    break;
                }
                case ILFlowControl.ConditionalJump:
                {
                    // We need to consider that the condition might be true or false.
                    
                    // Conditional branch(es): 
                    var metadata = InferJumpTargets(instruction);
                    if (metadata != null)
                    {
                        foreach (var target in metadata.InferredJumpTargets)
                        {
                            var branch = next.Copy();
                            branch.IP = target;
                            nextStates.Add(branch);
                            blockHeaders.Add((long) branch.IP);
                        }
                    }

                    // Fall through branch:
                    nextStates.Add(next);
                    blockHeaders.Add((long) next.IP);
                    
                    break;
                }
                case ILFlowControl.Return:
                {
                    blockHeaders.Add((long) next.IP);
                    // Return, do nothing.
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        private JumpMetadata InferJumpTargets(ILInstruction instruction)
        {
            try
            {
                var metadata = new JumpMetadata();
                var symbolicAddress = instruction.Dependencies[instruction.Dependencies.Count - 1];

                foreach (var dataSource in symbolicAddress.DataSources)
                {
                    var emulator = new InstructionEmulator();
                    emulator.EmulateDependentInstructions(dataSource);
                    emulator.EmulateInstruction(dataSource);
                    
                    // After partial emulation, IP is on stack.
                    var nextIp = emulator.Stack.Pop();
                    Logger.Debug(Tag, $"Inferred edge IL_{instruction.Offset:X4} -> IL_{nextIp.U8:X4}");
                    metadata.InferredJumpTargets.Add(nextIp.U8);
                }

                instruction.InferredMetadata = metadata;
                return metadata;
            }
            catch (NotSupportedException e)
            {
                Logger.Warning(Tag, "Could not infer jump target for " + instruction.Offset.ToString("X4") + ". " + e.Message);
            }

            return null;
        }
    }
}