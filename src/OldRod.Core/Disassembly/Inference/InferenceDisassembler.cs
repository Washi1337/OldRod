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
using AsmResolver;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Annotations;
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
            get => _vCallProcessor.Logger;
            set => _vCallProcessor.Logger = value;
        } 
        
        public ControlFlowGraph DisassembleExport(VMExportInfo export)
        {               
            // TODO: maybe reuse instructions and blockHeaders dictionary for speed up?
            
            var instructions = new Dictionary<long, ILInstruction>();
            var blockHeaders = new HashSet<long>();
                
            // Raw disassemble.
            Logger.Debug(Tag, $"Disassembling instructions...");
            Disassemble(instructions, blockHeaders, export);
                
            // Construct flow graph.
            Logger.Debug(Tag, $"Constructing CFG...");
            return ControlFlowGraphBuilder.BuildGraph(export, instructions.Values, blockHeaders);
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
                Key = exportInfo.EntryKey,
            };
            initialState.Stack.Push(new SymbolicValue(new ILInstruction(1, ILOpCodes.CALL, exportInfo.CodeOffset),
                VMType.Qword));
            
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
                foreach (var state in GetNextStates(blockHeaders, currentState, instruction, decoder))
                    agenda.Push(state);
            }
        }

        private IList<ProgramState> GetNextStates(
            ISet<long> blockHeaders, 
            ProgramState currentState, 
            ILInstruction instruction,
            InstructionDecoder decoder)
        {
            var nextStates = new List<ProgramState>(1);
            var next = currentState.Copy();
            next.IP += (ulong) instruction.Size;
            next.Key = decoder.CurrentKey;
            
            if (instruction.OpCode.AffectsFlags)
                next.Registers[VMRegisters.FL] = new SymbolicValue(instruction, VMType.Byte);

            switch (instruction.OpCode.Code)
            {
                case ILCode.CALL:
                    nextStates.AddRange(ProcessCall(instruction, next));
                    break;
                case ILCode.VCALL:
                    // VCalls have embedded opcodes with different behaviours.
                    nextStates.AddRange(_vCallProcessor.GetNextStates(instruction, next));
                    break;
                case ILCode.TRY:
                    // TRY opcodes have a very distinct behaviour from the other common opcodes.
                    nextStates.AddRange(ProcessTry(instruction, next));
                    blockHeaders.Add((long) next.IP);
                    break;
                case ILCode.LEAVE:
                    nextStates.AddRange(ProcessLeave(instruction, next));
                    blockHeaders.Add((long) next.IP);
                    break;
                default:
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

                    if (instruction.Annotation == null)
                        instruction.Annotation = new Annotation();
                
                    instruction.Annotation.InferredPopCount = popCount;
                    instruction.Annotation.InferredPushCount = pushCount;
                    break;
                }
            }

            return nextStates;
        }

        private IEnumerable<ProgramState> ProcessCall(ILInstruction instruction, ProgramState next)
        {
            int dependencyIndex = 0;
            
            var symbolicAddress = next.Stack.Pop();
            instruction.Dependencies.AddOrMerge(dependencyIndex++, symbolicAddress);

            ulong address = symbolicAddress.InferStackValue().U8;
            var entry = _koiStream.Exports.FirstOrDefault(x => x.Value.CodeOffset == address);

            if (entry.Value == null)
            {
                // TODO: infer call signature if the method does not have an export defined.
                throw new DisassemblyException(
                    $"Could not resolve signature of called method at offset IL_{instruction.Offset:X4}.",
                    new NotSupportedException("Calls to methods with no export defined are not supported yet."));
            }

            // Collect method arguments:
            var arguments = new List<SymbolicValue>();
            for (int i = 0; i < entry.Value.Signature.ParameterTokens.Count; i++)
                arguments.Add(next.Stack.Pop());
            if ((entry.Value.Signature.Flags & _constants.FlagInstance) != 0) 
                arguments.Add(next.Stack.Pop());
            
            arguments.Reverse();
            
            // Add argument dependencies.
            foreach (var argument in arguments)
                instruction.Dependencies.AddOrMerge(dependencyIndex++, argument);
            
            instruction.Annotation = new CallAnnotation
            {
                Address = address,
                Signature = entry.Value.Signature,
                ExportId = entry.Key,
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 0
            };

            return new[] {next};
        }

        private IEnumerable<ProgramState> ProcessTry(ILInstruction instruction, ProgramState next)
        {
            var result = new List<ProgramState> {next};

            int dependencyIndex = 0;         
            
            // Pop and infer handler type.
            var symbolicType = next.Stack.Pop();
            instruction.Dependencies.AddOrMerge(dependencyIndex++, symbolicType);

            var frame = new EHFrame
            {
                Type = _constants.EHTypes[symbolicType.InferStackValue().U1],
                TryStart = (ulong) instruction.Offset
            };
            next.EHStack.Push(frame);

            switch (frame.Type)
            {
                case EHType.CATCH:
                    // Pop and infer catch type.
                    var symbolicCatchType = next.Stack.Pop();
                    uint catchTypeId = symbolicCatchType.InferStackValue().U4;
                    frame.CatchType = (ITypeDefOrRef) _koiStream.ResolveReference(Logger, instruction.Offset, catchTypeId,
                        MetadataTokenType.TypeDef,
                        MetadataTokenType.TypeRef,
                        MetadataTokenType.TypeSpec);
                    instruction.Dependencies.AddOrMerge(dependencyIndex++, symbolicCatchType);
                    break;
                
                case EHType.FILTER:
                    // Pop and infer filter address.
                    var symbolicFilterAddress = next.Stack.Pop();
                    frame.FilterAddress = symbolicFilterAddress.InferStackValue().U8;
                    instruction.Dependencies.AddOrMerge(dependencyIndex++, symbolicFilterAddress);
                    break;
                
                case EHType.FAULT:
                    // KoiVM does not support fault clauses.
                    throw new NotSupportedException();
                
                case EHType.FINALLY:
                    // No extra values on the stack.
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Pop and infer handler address.
            var symbolicHandlerAddress = next.Stack.Pop();
            instruction.Dependencies.AddOrMerge(dependencyIndex, symbolicHandlerAddress);
            frame.HandlerAddress = symbolicHandlerAddress.InferStackValue().U8;

            // Branch to handler block.
            var handlerState = next.Copy();
            handlerState.Key = 0;
            handlerState.IP = frame.HandlerAddress;
            result.Add(handlerState);
            
            // Branch to filter block if necessary.
            if (frame.FilterAddress != 0)
            {
                var filterState = next.Copy();
                filterState.Key = 0;
                filterState.IP = frame.FilterAddress;
                result.Add(filterState);
            }

            instruction.Annotation = new Annotation
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 0,
            };
            
            return result;
        }

        private IEnumerable<ProgramState> ProcessLeave(ILInstruction instruction, ProgramState next)
        {
            // Not really necessary to resolve this to a value since KoiVM only uses this as some sort of sanity check.
            var symbolicHandler = next.Stack.Pop();
            instruction.Dependencies.AddOrMerge(0, symbolicHandler);

            next.EHStack.Pop();

            instruction.Annotation = new Annotation
            {
                InferredPopCount = 1,
                InferredPushCount = 0
            };
            
            return new[] {next};
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
                
                case ILStackBehaviour.PopVar:
                    
                    // Should never happen. Instructions with a variable amount of values popped from the stack
                    // are handled separately.
                    throw new DisassemblyException(
                        $"Attempted to infer static stack pop behaviour of a PopVar instruction at IL_{instruction.Offset:X4}.");
                    
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

        private JumpAnnotation InferJumpTargets(ILInstruction instruction)
        {
            try
            {
                var metadata = new JumpAnnotation();
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

                instruction.Annotation = metadata;
                return metadata;
            }
            catch (NotSupportedException e)
            {
                Logger.Warning(Tag, $"Could not infer jump target for {instruction.Offset:X4}. {e.Message}");
            }

            return null;
        }
        
    }
}