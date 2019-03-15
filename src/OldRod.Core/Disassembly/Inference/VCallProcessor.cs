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
using OldRod.Core.Disassembly.DataFlow;
using OldRod.Core.Emulation;

namespace OldRod.Core.Disassembly.Inference
{
    public class VCallProcessor
    {
        private const string Tag = "VCallProcessor";
        private readonly MetadataImage _image;
        private readonly VMConstants _constants;
        private readonly KoiStream _koiStream;
        
        public VCallProcessor(MetadataImage image, VMConstants constants, KoiStream koiStream)
        {
            _image = image ?? throw new ArgumentNullException(nameof(image));
            _constants = constants ?? throw new ArgumentNullException(nameof(constants));
            _koiStream = koiStream ?? throw new ArgumentNullException(nameof(koiStream));
        }

        public ILogger Logger
        {
            get;
            set;
        } = EmptyLogger.Instance;

        public IList<ProgramState> ProcessVCall(ILInstruction instruction, ProgramState next)
        {
            var nextStates = new List<ProgramState>(1);
            var metadata = instruction.InferredMetadata as VCallMetadata;

            int stackSize = next.Stack.Count;
            
            var symbolicVCallValue = next.Stack.Pop();
            instruction.Dependencies.AddOrMerge(0, symbolicVCallValue);
            var vcall = metadata?.VMCall ?? _constants.VMCalls[InferStackValue(symbolicVCallValue).U1];

            switch (vcall)
            {
                case VMCalls.ECALL:
                    ProcessECall(instruction, next);
                    break;
                case VMCalls.BOX:
                    ProcessBox(instruction, next);
                    break;
                case VMCalls.LDFLD:
                    ProcessLdfld(instruction, next);
                    break;
                case VMCalls.STFLD:
                    ProcessStfld(instruction, next);
                    break;
                case VMCalls.TOKEN:
                    ProcessToken(instruction, next);
                    break;
                case VMCalls.SIZEOF:
                    ProcessSizeOf(instruction, next);
                    break;
                case VMCalls.CAST:
                    ProcessCast(instruction, next);
                    break;
                case VMCalls.EXIT:
                case VMCalls.BREAK:
                case VMCalls.UNBOX:
                case VMCalls.LOCALLOC:
                case VMCalls.CKFINITE:
                case VMCalls.CKOVERFLOW:
                case VMCalls.RANGECHK:
                case VMCalls.INITOBJ:
                case VMCalls.LDFTN:
                case VMCalls.THROW:
                    throw new NotSupportedException($"VCALL {vcall} is not supported.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (vcall != VMCalls.EXIT)
                nextStates.Add(next);

            if ((next.Stack.Count - stackSize) != instruction.InferredMetadata.InferredStackDelta)
            {
                // Should not happen, but sanity checks are always nice to check whether we have implemented the 
                // vcall processors correctly.
                throw new DisassemblyException($"VCall at offset IL_{instruction.Offset:X4} ({vcall}) inferred stack delta does not match the emulated stack delta.");
            }
            
            return nextStates;
        }

        private IMetadataMember ResolveReference(ILInstruction instruction, VMCalls call, uint id, params MetadataTokenType[] expectedMembers)
        {
            if (!_koiStream.References.TryGetValue(id, out var token))
            {
                throw new DisassemblyException($"Detected an invalid reference ID on the stack " +
                                               $"used for resolution at offset IL_{instruction.Offset:X4}.");
            }

            if (!expectedMembers.Contains(token.TokenType))
            {
                Logger.Warning(Tag,
                    $"Detected a reference to a {token.TokenType} member that cannot be used with a {call} VCall.");
            }

            try
            {
                return _image.ResolveMember(token);
            }
            catch (MemberResolutionException ex)
            {
                throw new DisassemblyException(
                    $"Could not resolve the member {token} referenced in the {call} VCall at offset {instruction.Offset:X4}.",
                    ex);
            }
        }

        private void ProcessBox(ILInstruction instruction, ProgramState next)
        {
            // Pop arguments and add dependencies.
            var symbolicType = next.Stack.Pop();
            var symbolicValue = next.Stack.Pop();

            instruction.Dependencies.AddOrMerge(1, symbolicValue);
            instruction.Dependencies.AddOrMerge(2, symbolicType);

            next.Stack.Push(new SymbolicValue(instruction, VMType.Object));
            
            // Infer type.
            uint typeId = InferStackValue(symbolicType).U4;
            var type = (ITypeDefOrRef) ResolveReference(instruction, VMCalls.BOX, typeId,
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
            
            var valueSlot = InferStackValue(symbolicValue);            
            object value;
            if (type.IsTypeOf("System", "String"))
                value = _koiStream.Strings[valueSlot.U4];
            else
                value = null; // TODO: infer value types.

            
            // Add metadata
            instruction.InferredMetadata = new BoxMetadata(type, value)
            {
                InferredPopCount = instruction.Dependencies.Count
            };
        }

        private void ProcessECall(ILInstruction instruction, ProgramState next)
        {
            int index = 1;
            
            // Pop raw method value from stack.
            var symbolicMethod = next.Stack.Pop();
            instruction.Dependencies.AddOrMerge(index++, symbolicMethod);
            
            // Infer method and opcode used.
            var methodSlot = InferStackValue(symbolicMethod);
            uint methodId = methodSlot.U4 & 0x3fffffff;
            var opCode = _constants.ECallOpCodes[(byte) (methodSlot.U4 >> 30)];
            var method = (IMethodDefOrRef) ResolveReference(instruction, VMCalls.ECALL, methodId,
                MetadataTokenType.Method, MetadataTokenType.MethodSpec, MetadataTokenType.MemberRef);
            var methodSignature = (MethodSignature) method.Signature;

            // Collect method arguments:
            var arguments = new List<SymbolicValue>();
            for (int i = 0; i < methodSignature.Parameters.Count; i++)
                arguments.Add(next.Stack.Pop());
            if (method.Signature.HasThis && opCode != VMECallOpCode.ECALL_NEWOBJ)
                arguments.Add(next.Stack.Pop());
            
            arguments.Reverse();
            
            // Add argument dependencies.
            foreach (var argument in arguments)
                instruction.Dependencies.AddOrMerge(index++, argument);
            
            // Push result, if any.
            bool hasResult = !methodSignature.ReturnType.IsTypeOf("System", "Void")
                             || opCode == VMECallOpCode.ECALL_NEWOBJ;
            if (hasResult)
            {
                next.Stack.Push(new SymbolicValue(instruction, methodSignature.ReturnType.ToVMType()));
            }

            // Add metadata
            instruction.InferredMetadata = new ECallMetadata(method, opCode)
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
            uint fieldId = InferStackValue(symbolicField).U4;
            var field = (ICallableMemberReference) ResolveReference(instruction, VMCalls.LDFLD, fieldId,
                MetadataTokenType.Field, MetadataTokenType.MemberRef);
            var fieldSig = (FieldSignature) field.Signature;
            
            // Add dependencies.
            instruction.Dependencies.AddOrMerge(1, symbolicField);
            instruction.Dependencies.AddOrMerge(2, symbolicObject);

            // Push field value.
            next.Stack.Push(new SymbolicValue(instruction, fieldSig.FieldType.ToVMType()));
            
            // Create metadata.
            instruction.InferredMetadata = new FieldMetadata(VMCalls.LDFLD, field)
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
            uint fieldId = InferStackValue(symbolicField).U4;
            var field = (ICallableMemberReference) ResolveReference(instruction, VMCalls.LDFLD, fieldId,
                MetadataTokenType.Field, MetadataTokenType.MemberRef);

            // Add dependencies.
            instruction.Dependencies.AddOrMerge(1, symbolicField);
            instruction.Dependencies.AddOrMerge(2, symbolicObject);
            instruction.Dependencies.AddOrMerge(3, symbolicValue);

            // Create metadata.
            instruction.InferredMetadata = new FieldMetadata(VMCalls.STFLD, field)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 0
            };
        }

        private void ProcessToken(ILInstruction instruction, ProgramState next)
        {
            var symbolicToken = next.Stack.Pop();
            
            // Resolve member.
            uint memberId = InferStackValue(symbolicToken).U4;
            var member = ResolveReference(instruction, VMCalls.TOKEN, memberId,
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
            instruction.InferredMetadata = new TokenMetadata(member)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 1
            };
        }

        private void ProcessSizeOf(ILInstruction instruction, ProgramState next)
        {
            var symbolicType = next.Stack.Pop();

            uint typeId = InferStackValue(symbolicType).U4;
            var type = (ITypeDefOrRef) ResolveReference(instruction, VMCalls.SIZEOF, typeId,
                MetadataTokenType.TypeDef,
                MetadataTokenType.TypeRef,
                MetadataTokenType.TypeSpec);

            instruction.Dependencies.AddOrMerge(1, symbolicType);
            
            next.Stack.Push(new SymbolicValue(instruction, VMType.Dword));

            instruction.InferredMetadata = new TypeMetadata(VMCalls.SIZEOF, type)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 1
            };
        }

        private void ProcessCast(ILInstruction instruction, ProgramState next)
        {
            var symbolicType = next.Stack.Pop();
            var symbolicValue = next.Stack.Pop();

            uint typeId = InferStackValue(symbolicType).U4;
            bool isSafeCast = (typeId & 0x80000000) == 0;
            var type = (ITypeDefOrRef) ResolveReference(instruction, VMCalls.CAST, typeId & ~0x80000000,
                MetadataTokenType.TypeDef,
                MetadataTokenType.TypeRef,
                MetadataTokenType.TypeSpec);

            instruction.Dependencies.AddOrMerge(1, symbolicType);
            instruction.Dependencies.AddOrMerge(2, symbolicValue);
            
            next.Stack.Push(new SymbolicValue(instruction, VMType.Object));

            instruction.InferredMetadata = new CastMetadata(type, isSafeCast)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = 1
            };
        }

        private static VMSlot InferStackValue(SymbolicValue symbolicValue)
        {
            var emulator = new InstructionEmulator();
            var pushValue = symbolicValue.DataSources.First(); // TODO: might need to verify multiple data sources.
            emulator.EmulateDependentInstructions(pushValue);
            emulator.EmulateInstruction(pushValue);
            return emulator.Stack.Pop();
        }
    }
}