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
using AsmResolver.Net;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.DataFlow;
using OldRod.Core.Emulation;

namespace OldRod.Core.Disassembly.Inference
{
    public class InstructionProcessor
    {
        public const string Tag = "InstructionProcessor";
        
        private readonly InferenceDisassembler _disassembler;
        private readonly VCallProcessor _vCallProcessor;

        public InstructionProcessor(InferenceDisassembler disassembler)
        {
            _disassembler = disassembler;
            _vCallProcessor = new VCallProcessor(disassembler);
        }

        private ILogger Logger => _disassembler.Logger;
        private KoiStream KoiStream => _disassembler.KoiStream;
        private VMConstants Constants => _disassembler.Constants;

        public IList<ProgramState> GetNextStates(
            VMFunction function,
            ProgramState currentState,
            ILInstruction instruction,
            uint nextKey)
        {
            var nextStates = new List<ProgramState>(1);
            var next = currentState.Copy();
            next.IP += (ulong) instruction.Size;
            next.Key = nextKey;
            
            if (instruction.OpCode.AffectsFlags)
                next.Registers[VMRegisters.FL] = new SymbolicValue(instruction, VMType.Byte);

            switch (instruction.OpCode.Code)
            {
                case ILCode.CALL:
                    nextStates.AddRange(ProcessCall(function, instruction, next));
                    break;
                case ILCode.RET:
                    ProcessRet(function, instruction, next);
                    break;
                case ILCode.VCALL:
                    // VCalls have embedded opcodes with different behaviours.
                    nextStates.AddRange(_vCallProcessor.GetNextStates(function, instruction, next));
                    break;
                case ILCode.TRY:
                    // TRY opcodes have a very distinct behaviour from the other common opcodes.
                    nextStates.AddRange(ProcessTry(instruction, next));
                    break;
                case ILCode.LEAVE:
                    nextStates.AddRange(ProcessLeave(function, instruction, next));
                    function.BlockHeaders.Add((long) next.IP);
                    break;
                case ILCode.POP when (VMRegisters) instruction.Operand == VMRegisters.SP:
                    nextStates.Add(ProcessPopSp(instruction, next));
                    break;
                default:
                {
                    // Push/pop necessary values from stack.
                    int initial = next.Stack.Count;
                    PopSymbolicValues(function, instruction, next);
                    int popCount = initial - next.Stack.Count;

                    initial = next.Stack.Count;
                    PushSymbolicValues(instruction, next);
                    int pushCount = next.Stack.Count - initial;
                
                    // Apply control flow.
                    PerformFlowControl(function, instruction, nextStates, next);

                    if (instruction.Annotation == null)
                        instruction.Annotation = new Annotation();
                
                    instruction.Annotation.InferredPopCount = popCount;
                    instruction.Annotation.InferredPushCount = pushCount;
                    break;
                }
            }

            return nextStates;
        }

        private IEnumerable<ProgramState> ProcessCall(VMFunction function, ILInstruction instruction, ProgramState next)
        {
            var symbolicAddress = next.Stack.Pop();
            instruction.Dependencies.AddOrMerge(0, symbolicAddress);

            uint address = (uint) symbolicAddress.InferStackValue().U8;

            if (address >= KoiStream.Data.Length)
            {
                Logger.Warning(Tag,
                    $"Call instruction at IL_{instruction.Offset:X4} "
                    + $"transfers control to a function outside of the KoiVM stream (IL_{address:X4}.");
            }

            var callee = _disassembler.GetOrCreateFunctionInfo(address, next.Key);

            callee.References.Add(new FunctionReference(function, instruction.Offset, FunctionReferenceType.Call, callee));
            instruction.Annotation = new CallAnnotation
            {
                Function = callee,
                InferredPopCount = 1,
                InferredPushCount = 1,
            };

            if (!callee.ExitKey.HasValue)
            {
                // Exit key of called function is not known yet.
                // We cannot continue disassembly yet because of the encryption used in KoiVM.
                function.UnresolvedOffsets.Add(instruction.Offset);
                Logger.Debug(Tag,
                    $"Stopped at call instruction at IL_{instruction.Offset:X4} "
                    + $"as exit key of function_{address:X4} is not known yet.");
                return Enumerable.Empty<ProgramState>();
            }
            else
            {
                // Exit key is known, we can continue disassembly!
                function.UnresolvedOffsets.Remove(instruction.Offset);
                next.Key = callee.ExitKey.Value;         

                return new[] {next};
            }
        }

        private void ProcessRet(VMFunction function, ILInstruction instruction, ProgramState next)
        {
            // Pop return address.
            var symbolicReturnAddress = next.Stack.Pop();
            instruction.Dependencies.AddOrMerge(0, symbolicReturnAddress);

            // Add metadata.
            instruction.Annotation = new Annotation
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 0
            };

            // Returns indicate the end of the export, and therefore also determine the encryption key of the 
            // instruction after a call instruction. Store this information so it can be used to continue
            // disassembly at these points later in time.
            
            if (function.ExitKey.HasValue)
            {
                // Assuming any call can trigger any execution path in the CFG, any return must fix up to the same
                // exit key. 
                
                if (function.ExitKey != next.Key)
                {
                    // This should not happen in vanilla KoiVM. 
                    Logger.Warning(Tag,
                        $"Resolved an exit key ({next.Key:X8}) at offset IL_{instruction.Offset:X4} "
                        + $"that is different from the previously resolved exit key ({function.ExitKey:X8}).");
                }
            }
            else
            {
                Logger.Debug(Tag, $"Inferred exit key {next.Key:X8}.");
                function.ExitKey = next.Key;
            }
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
                Type = Constants.EHTypes[symbolicType.InferStackValue().U1],
                TryStart = (ulong) instruction.Offset
            };
            next.EHStack.Push(frame);

            switch (frame.Type)
            {
                case EHType.CATCH:
                    // Pop and infer catch type.
                    var symbolicCatchType = next.Stack.Pop();
                    uint catchTypeId = symbolicCatchType.InferStackValue().U4;
                    frame.CatchType = (ITypeDefOrRef) KoiStream.ResolveReference(Logger, instruction.Offset, catchTypeId,
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

        private IEnumerable<ProgramState> ProcessLeave(VMFunction disassembly, ILInstruction instruction, ProgramState next)
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

        private ProgramState ProcessPopSp(ILInstruction instruction, ProgramState next)
        {
            int oldValue = next.Stack.Count;
            var symbolicValue = next.Stack.Pop();
            int newValue = (int) symbolicValue.InferStackValue().U4;
            int difference = (newValue + 1) - oldValue;

            instruction.Dependencies.AddOrMerge(0, symbolicValue);
            instruction.Annotation = new Annotation
            {
                InferredPopCount = 1
            };
            
            if (difference > 0)
            {
                // Allocation of new stack slots.
                for (int i = 0; i < difference; i++) 
                    next.Stack.Push(new SymbolicValue(instruction, VMType.Unknown));
                instruction.Annotation.InferredPushCount = difference;
            }
            else if (difference < 0) 
            {
                // Popping multiple values from the stack.
                for (int i = 0; i < -difference; i++)
                    next.Stack.Pop();
                instruction.Annotation.InferredPopCount += -difference;
            }
            
            return next;
        }

        private void PopSymbolicValues(VMFunction disassembly, ILInstruction instruction, ProgramState next)
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

        private void PerformFlowControl(VMFunction disassembly, ILInstruction instruction, List<ProgramState> nextStates, ProgramState next)
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
                    disassembly.BlockHeaders.Add((long) next.IP);
                    
                    // Unconditional jump target.
                    var metadata = InferJumpTargets(instruction);
                    if (metadata != null)
                    {
                        next.IP = metadata.InferredJumpTargets[0];
                        disassembly.BlockHeaders.Add((long) next.IP);
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
                            disassembly.BlockHeaders.Add((long) branch.IP);
                        }
                    }

                    // Fall through branch:
                    nextStates.Add(next);
                    disassembly.BlockHeaders.Add((long) next.IP);
                    
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
                    uint nextIp = (uint) emulator.Stack.Pop().U8;
                    
                    Logger.Debug(Tag, $"Inferred edge IL_{instruction.Offset:X4} -> IL_{nextIp:X4}");

                    if (nextIp > (ulong) KoiStream.Data.Length)
                    {
                        Logger.Warning(Tag,
                            $"Jump instruction at IL_{instruction.Offset:X4} "
                            + $"transfers control to an instruction outside of the KoiVM stream (IL_{nextIp:X4}.");
                    }
                    
                    metadata.InferredJumpTargets.Add(nextIp);
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