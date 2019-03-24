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
using System.Linq;
using AsmResolver.Net;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Memory
{
    public class DefaultFrameLayoutDetector : IFrameLayoutDetector
    {
        public IFrameLayout DetectFrameLayout(VMConstants constants, VMFunction function)
        {
            if (function.References.Count == 0)
                throw new ArgumentException("Can only infer frame layout of a function that is at least referenced once.");

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

            var reference = function.References.First();

            // Find the POP SP instruction.
            int currentOffset = reference.Offset;
            ILInstruction instruction;
            do
            {
                instruction = reference.Caller.Instructions[currentOffset];
                currentOffset += instruction.Size;
            } while (instruction.OpCode.Code != ILCode.POP || (VMRegisters) instruction.Operand != VMRegisters.SP);

            // The number of arguments pushed onto the stack is the number of values implicitly popped from the stack
            // at this POP SP instruction.
            int argumentCount = instruction.Annotation.InferredPopCount - 1;
            
            return new DefaultFrameLayout(argumentCount, 0, true);
        }

        public IFrameLayout DetectFrameLayout(VMConstants constants, MetadataImage image, VMExportInfo export)
        {
            var returnType = (ITypeDefOrRef) image.ResolveMember(export.Signature.ReturnToken);
            return new DefaultFrameLayout(
                export.Signature.ParameterTokens.Count, 
                0, 
                !returnType.IsTypeOf("System", "Void"));
        }
    }
}