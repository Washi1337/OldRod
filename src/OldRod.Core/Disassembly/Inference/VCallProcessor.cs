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
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.DataFlow;
using OldRod.Core.Emulation;

namespace OldRod.Core.Disassembly.Inference
{
    public class VCallProcessor
    {
        private const string Tag = "VCallProcessor";
        
        private readonly InferenceDisassembler _disassembler;

        public VCallProcessor(InferenceDisassembler disassembler)
        {
            _disassembler = disassembler ?? throw new ArgumentNullException(nameof(disassembler));
        }

        private ILogger Logger => _disassembler.Logger;
        private VMConstants Constants => _disassembler.Constants;
        private KoiStream KoiStream => _disassembler.KoiStream;
        
        public IList<ProgramState> GetNextStates(ILInstruction instruction, ProgramState next)
        {
            var nextStates = new List<ProgramState>(1);
            var metadata = instruction.Annotation as VCallAnnotation;

            int stackSize = next.Stack.Count;
            
            var symbolicVCallValue = next.Stack.Pop();
            instruction.Dependencies.AddOrMerge(0, symbolicVCallValue);
            var vcall = metadata?.VMCall ?? Constants.VMCalls[symbolicVCallValue.InferStackValue().U1];

            switch (vcall)
            {
                case VMCalls.BOX:
                    ProcessBox(instruction, next);
                    break;
                case VMCalls.CAST:
                    ProcessCast(instruction, next);
                    break;
                case VMCalls.ECALL:
                    ProcessECall(instruction, next);
                    break;
                case VMCalls.INITOBJ:
                    ProcessInitObj(instruction, next);
                    break;
                case VMCalls.LOCALLOC:
                    ProcessLocalloc(instruction, next);
                    break;
                case VMCalls.LDFLD:
                    ProcessLdfld(instruction, next);
                    break;
                case VMCalls.RANGECHK:
                    ProcessRangeChk(instruction, next);
                    break;
                case VMCalls.SIZEOF:
                    ProcessSizeOf(instruction, next);
                    break;
                case VMCalls.STFLD:
                    ProcessStfld(instruction, next);
                    break;
                case VMCalls.TOKEN:
                    ProcessToken(instruction, next);
                    break;
                case VMCalls.UNBOX:
                    ProcessUnbox(instruction, next);
                    break;
                case VMCalls.EXIT:
                case VMCalls.BREAK:
                case VMCalls.CKFINITE:
                case VMCalls.CKOVERFLOW:
                case VMCalls.LDFTN:
                case VMCalls.THROW:
                    throw new NotSupportedException($"VCALL {vcall} is not supported.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (vcall != VMCalls.EXIT)
                nextStates.Add(next);

            if ((next.Stack.Count - stackSize) != instruction.Annotation.InferredStackDelta)
            {
                // Should not happen, but sanity checks are always nice to check whether we have implemented the 
                // vcall processors correctly.
                throw new DisassemblyException($"VCall at offset IL_{instruction.Offset:X4} ({vcall}) inferred stack delta does not match the emulated stack delta.");
            }
            
            return nextStates;
        }

        private void ProcessBox(ILInstruction instruction, ProgramState next)
        {
            // Pop arguments and add dependencies.
            var symbolicType = next.Stack.Pop();
            var symbolicValue = next.Stack.Pop();

            instruction.Dependencies.AddOrMerge(1, symbolicType);
            instruction.Dependencies.AddOrMerge(2, symbolicValue);

            next.Stack.Push(new SymbolicValue(instruction, VMType.Object));
            
            // Infer type.
            uint typeId = symbolicType.InferStackValue().U4;
            var type = (ITypeDefOrRef) KoiStream.ResolveReference(Logger, instruction.Offset, typeId,
                MetadataTokenType.TypeDef,
                MetadataTokenType.TypeRef,
                MetadataTokenType.TypeSpec);

            // Infer value.
            
            // TODO: Box values pushed onto the stack might not be constants, but might be part of 
            //       a normal conversion of a stack value from value type to object. We need a way
            //       to detect this and encode this in the metadata somehow.
            //
            //       Perhaps by checking whether registers or stack slots are used in the dependent
            //       instructions?
                       
            object value;
            if (type.IsTypeOf("System", "String"))
            {
                var valueSlot = symbolicValue.InferStackValue();
                value = KoiStream.Strings[valueSlot.U4];
            }
            else
            {
                value = null; // TODO: infer value types.
            }

            // Add metadata
            instruction.Annotation = new BoxAnnotation(type, value)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 1
            };
        }

        private void ProcessECall(ILInstruction instruction, ProgramState next)
        {
            int index = 1;
            
            // Pop raw method value from stack.
            var symbolicMethod = next.Stack.Pop();
            instruction.Dependencies.AddOrMerge(index++, symbolicMethod);
            
            // Infer method and opcode used.
            var methodSlot = symbolicMethod.InferStackValue();
            uint methodId = methodSlot.U4 & 0x3fffffff;
            var opCode = Constants.ECallOpCodes[(byte) (methodSlot.U4 >> 30)];
            var method = (ICallableMemberReference) KoiStream.ResolveReference(Logger, instruction.Offset, methodId,
                MetadataTokenType.Method, MetadataTokenType.MethodSpec, MetadataTokenType.MemberRef);
            var methodSignature = (MethodSignature) method.Signature;

            // Collect method arguments:
            var arguments = new List<SymbolicValue>();
            for (int i = 0; i < methodSignature.Parameters.Count; i++)
                arguments.Add(next.Stack.Pop());
            if (method.Signature.HasThis && opCode != VMECallOpCode.NEWOBJ)
                arguments.Add(next.Stack.Pop());
            
            arguments.Reverse();
            
            // Add argument dependencies.
            foreach (var argument in arguments)
                instruction.Dependencies.AddOrMerge(index++, argument);
            
            // Push result, if any.
            bool hasResult = !methodSignature.ReturnType.IsTypeOf("System", "Void")
                             || opCode == VMECallOpCode.NEWOBJ;
            if (hasResult)
            {
                next.Stack.Push(new SymbolicValue(instruction, methodSignature.ReturnType.ToVMType()));
            }

            // Add metadata
            instruction.Annotation = new ECallAnnotation(method, opCode)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = hasResult ? 1 : 0
            };
        }

        private void ProcessLdfld(ILInstruction instruction, ProgramState next)
        {
            var symbolicField = next.Stack.Pop();
            var symbolicObject = next.Stack.Pop();

            // Resolve field.
            uint fieldId = symbolicField.InferStackValue().U4;
            var field = (ICallableMemberReference) KoiStream.ResolveReference(Logger, instruction.Offset, fieldId,
                MetadataTokenType.Field, MetadataTokenType.MemberRef);
            var fieldSig = (FieldSignature) field.Signature;
            
            // Add dependencies.
            instruction.Dependencies.AddOrMerge(1, symbolicField);
            instruction.Dependencies.AddOrMerge(2, symbolicObject);

            // Push field value.
            next.Stack.Push(new SymbolicValue(instruction, fieldSig.FieldType.ToVMType()));
            
            // Create metadata.
            instruction.Annotation = new FieldAnnotation(VMCalls.LDFLD, field)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 1
            };
        }

        private void ProcessStfld(ILInstruction instruction, ProgramState next)
        {
            var symbolicField = next.Stack.Pop();
            var symbolicValue = next.Stack.Pop();
            var symbolicObject = next.Stack.Pop();

            // Resolve field.
            uint fieldId = symbolicField.InferStackValue().U4;
            var field = (ICallableMemberReference) KoiStream.ResolveReference(Logger, instruction.Offset, fieldId,
                MetadataTokenType.Field, MetadataTokenType.MemberRef);

            // Add dependencies.
            instruction.Dependencies.AddOrMerge(1, symbolicField);
            instruction.Dependencies.AddOrMerge(2, symbolicObject);
            instruction.Dependencies.AddOrMerge(3, symbolicValue);

            // Create metadata.
            instruction.Annotation = new FieldAnnotation(VMCalls.STFLD, field)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 0
            };
        }

        private void ProcessToken(ILInstruction instruction, ProgramState next)
        {
            var symbolicToken = next.Stack.Pop();
            
            // Resolve member.
            uint memberId = symbolicToken.InferStackValue().U4;
            var member = KoiStream.ResolveReference(Logger, instruction.Offset, memberId,
                MetadataTokenType.TypeRef,
                MetadataTokenType.TypeDef,
                MetadataTokenType.TypeSpec,
                MetadataTokenType.Method,
                MetadataTokenType.MethodSpec,
                MetadataTokenType.Field,
                MetadataTokenType.MemberRef);
            
            // Add dependencies.
            instruction.Dependencies.AddOrMerge(1, symbolicToken);
            
            // Push result.
            next.Stack.Push(new SymbolicValue(instruction, VMType.Pointer));

            // Create metadata.
            instruction.Annotation = new TokenAnnotation(member)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 1
            };
        }

        private void ProcessSizeOf(ILInstruction instruction, ProgramState next)
        {
            var symbolicType = next.Stack.Pop();

            // Resolve type.
            uint typeId = symbolicType.InferStackValue().U4;
            var type = (ITypeDefOrRef) KoiStream.ResolveReference(Logger, instruction.Offset, typeId,
                MetadataTokenType.TypeDef,
                MetadataTokenType.TypeRef,
                MetadataTokenType.TypeSpec);

            // Add dependency.
            instruction.Dependencies.AddOrMerge(1, symbolicType);
            
            // Push value.
            next.Stack.Push(new SymbolicValue(instruction, VMType.Dword));

            // Add metadata.
            instruction.Annotation = new TypeAnnotation(VMCalls.SIZEOF, type)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 1
            };
        }

        private void ProcessCast(ILInstruction instruction, ProgramState next)
        {
            var symbolicType = next.Stack.Pop();
            var symbolicValue = next.Stack.Pop();

            // Resolve type and cast safety.
            uint typeId = symbolicType.InferStackValue().U4;
            bool isSafeCast = (typeId & 0x80000000) == 0;
            var type = (ITypeDefOrRef) KoiStream.ResolveReference(Logger, instruction.Offset,typeId & ~0x80000000,
                MetadataTokenType.TypeDef,
                MetadataTokenType.TypeRef,
                MetadataTokenType.TypeSpec);

            // Add dependencies.
            instruction.Dependencies.AddOrMerge(1, symbolicType);
            instruction.Dependencies.AddOrMerge(2, symbolicValue);
            
            // Push new value.
            next.Stack.Push(new SymbolicValue(instruction, VMType.Object));

            // Add metadata.
            instruction.Annotation = new CastAnnotation(type, isSafeCast)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 1
            };
        }

        private void ProcessUnbox(ILInstruction instruction, ProgramState next)
        {
            var symbolicType = next.Stack.Pop();
            var symbolicValue = next.Stack.Pop();

            // Resolve type and unbox kind.
            uint typeId = symbolicType.InferStackValue().U4;
            bool isUnboxPtr = (typeId & 0x80000000) == 0;
            var type = (ITypeDefOrRef) KoiStream.ResolveReference(Logger, instruction.Offset, typeId & ~0x80000000,
                MetadataTokenType.TypeDef,
                MetadataTokenType.TypeRef,
                MetadataTokenType.TypeSpec);

            // Add dependencies.
            instruction.Dependencies.AddOrMerge(1, symbolicType);
            instruction.Dependencies.AddOrMerge(2, symbolicValue);
            
            // Push new value.
            next.Stack.Push(new SymbolicValue(instruction, VMType.Object));

            // Add metadata.
            instruction.Annotation = new UnboxAnnotation(type, isUnboxPtr)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 1
            };
        }

        private void ProcessInitObj(ILInstruction instruction, ProgramState next)
        {
            var symbolicType = next.Stack.Pop();
            var symbolicValue = next.Stack.Pop();
            
            // Resolve type.
            uint typeId = symbolicType.InferStackValue().U4;
            var type = (ITypeDefOrRef) KoiStream.ResolveReference(Logger, instruction.Offset, typeId,
                MetadataTokenType.TypeDef,
                MetadataTokenType.TypeRef,
                MetadataTokenType.TypeSpec);

            instruction.Dependencies.AddOrMerge(1, symbolicType);
            instruction.Dependencies.AddOrMerge(2, symbolicValue);
            
            instruction.Annotation = new TypeAnnotation(VMCalls.INITOBJ, type)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 0
            };
        }

        private void ProcessRangeChk(ILInstruction instruction, ProgramState next)
        {
            // Pop arguments.
            var symbolicValue = next.Stack.Pop();
            var symbolicMax = next.Stack.Pop();
            var symbolicMin = next.Stack.Pop();
            
            // Add dependencies.
            instruction.Dependencies.AddOrMerge(1, symbolicValue);
            instruction.Dependencies.AddOrMerge(2, symbolicMax);
            instruction.Dependencies.AddOrMerge(3, symbolicMin);

            // Push result.
            next.Stack.Push(new SymbolicValue(instruction, VMType.Qword));
            
            // Add metadata.
            instruction.Annotation = new VCallAnnotation(VMCalls.RANGECHK, VMType.Qword)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 1
            };
        }

        private void ProcessLocalloc(ILInstruction instruction, ProgramState next)
        {
            // Pop arguments.
            var symbolicLength = next.Stack.Pop();
            
            // Add dependencies.
            instruction.Dependencies.AddOrMerge(1, symbolicLength);

            // Push result.
            next.Stack.Push(new SymbolicValue(instruction, VMType.Qword));
            
            // Add metadata.
            instruction.Annotation = new VCallAnnotation(VMCalls.LOCALLOC, VMType.Pointer)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 1
            };
        }
        
    }
}