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
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Memory
{
    public class DefaultFrameLayoutDetector : IFrameLayoutDetector
    {
        public const string Tag = "FrameLayoutDetector";
        
        public IFrameLayout DetectFrameLayout(VMConstants constants, MetadataImage image,
            VMFunction function)
        {
            if (function.References.Count == 0)
                throw new ArgumentException("Can only infer frame layout of a function that is at least referenced once.");

            var exceptions = new List<Exception>();
            
            // Order the references by reference type, as LDFTN references are more reliable.
            foreach (var reference in function.References.OrderBy(r => r.ReferenceType))
            {
                try
                {
                    switch (reference.ReferenceType)
                    {
                        case FunctionReferenceType.Call:
                            return InferLayoutFromCallReference(image, reference);
                        case FunctionReferenceType.Ldftn:
                            return InferLayoutFromLdftnReference(image, reference);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(
                $"Failed to infer the stack frame layout of function_{function.EntrypointAddress:X4}.", exceptions);
        }

        private static IFrameLayout InferLayoutFromLdftnReference(MetadataImage image, FunctionReference reference)
        {
            // LDFTN instructions reference a physical method, or an export defined in the export table containing
            // the signature of an intra-linked method. We can therefore reliably extract the necessary information
            // without too much guessing.
            
            var ldftn = reference.Caller.Instructions[reference.Offset];
            var annotation = (LdftnAnnotation) ldftn.Annotation;

            var parameterTypes = new List<TypeSignature>();
            TypeSignature returnType;
            
            if (annotation.IsIntraLinked)
            {
                returnType = ((ITypeDefOrRef) image.ResolveMember(annotation.Signature.ReturnToken))
                    .ToTypeSignature();

                foreach (var token in annotation.Signature.ParameterTokens)
                {
                    parameterTypes.Add(((ITypeDefOrRef) image.ResolveMember(token))
                        .ToTypeSignature());
                }
            }
            else
            {
                var methodSig = (MethodSignature) annotation.Method.Signature;
                foreach (var parameter in methodSig.Parameters)
                    parameterTypes.Add(parameter.ParameterType);
                returnType = methodSig.ReturnType;
            }

            return new DefaultFrameLayout(
                image,
                parameterTypes,
                Array.Empty<TypeSignature>(),
                returnType);
        }

        private static IFrameLayout InferLayoutFromCallReference(MetadataImage image, FunctionReference reference)
        {
            // This is kind of a hack, but works perfectly fine for vanilla KoiVM.  
            //
            // Vanilla KoiVM uses the calling convention where the caller cleans up the stack after the call.
            // The assumption is that each post-call code of the function is using the default calling convention is
            // in some variation of the following code: 
            // 
            //    CALL                                  ; Original call instruction.
            //
            //    PUSHR_DWORD R0                        ; Only present if the function returns something.      
            //    POP R0
            //
            //    PUSHR_DWORD SP                        ; Clean up of arguments on the stack.  
            //    PUSHI_DWORD <number of parameters>      
            //    ADD_DWORD                 
            //    POP SP
            //
            // Note that forks can deviate from this.
            //

            // Find the POP SP instruction.
            bool returnsValue = false;
            
            int currentOffset = reference.Offset;
            ILInstruction instruction;
            do
            {
                if (!reference.Caller.Instructions.TryGetValue(currentOffset, out instruction))
                {
                    throw new FrameLayoutDetectionException(
                        $"Could not infer the number of arguments of function_{reference.Callee.EntrypointAddress:X4} " +
                        $"due to an incomplete or unsupported post-call of IL_{reference.Offset:X4} (function_{reference.Caller.EntrypointAddress:X4}).",
                        new DisassemblyException(
                            $"Offset IL_{currentOffset:X4} is not disassembled or does not belong to function_{reference.Caller.EntrypointAddress:X4}."));
                }

                if (instruction.OpCode.Code == ILCode.PUSHR_DWORD
                    && (VMRegisters) instruction.Operand == VMRegisters.R0)
                {
                    returnsValue = true;
                }

                currentOffset += instruction.Size;
            } while (instruction.OpCode.Code != ILCode.POP || (VMRegisters) instruction.Operand != VMRegisters.SP);

            // The number of arguments pushed onto the stack is the number of values implicitly popped from the stack
            // at this POP SP instruction.
            int argumentCount = instruction.Annotation.InferredPopCount - 1;

            return new DefaultFrameLayout(
                image,
                Enumerable.Repeat<TypeSignature>(null, argumentCount).ToList(),
                Array.Empty<TypeSignature>(),
                returnsValue ? image.TypeSystem.Object : image.TypeSystem.Void);
        }

        public IFrameLayout DetectFrameLayout(VMConstants constants, MetadataImage image, VMExportInfo export)
        {   
            var parameterTypes = new List<TypeSignature>();
            foreach (var token in export.Signature.ParameterTokens)
            {
                parameterTypes.Add(((ITypeDefOrRef) image.ResolveMember(token))
                    .ToTypeSignature());
            }

            var returnType = ((ITypeDefOrRef) image.ResolveMember(export.Signature.ReturnToken))
                .ToTypeSignature();
            
            return new DefaultFrameLayout(
                image,
                parameterTypes,
                Array.Empty<TypeSignature>(),
                returnType);
        }
        
    }
}