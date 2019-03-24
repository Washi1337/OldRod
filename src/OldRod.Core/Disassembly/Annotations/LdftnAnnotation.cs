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

using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Disassembly.Annotations
{
    public class LdftnAnnotation : VCallAnnotation
    {
        public LdftnAnnotation(VMFunction function, VMFunctionSignature signature) 
            : base(VMCalls.LDFTN, VMType.Pointer)
        {
            Function = function;
            Signature = signature;
            Method = null;
        }

        public LdftnAnnotation(ICallableMemberReference method) 
            : base(VMCalls.LDFTN, VMType.Pointer)
        {
            Function = null;
            Signature = null;
            Method = method;
        }

        public bool IsIntraLinked => Function != null;

        public VMFunction Function
        {
            get;
        }

        public VMFunctionSignature Signature
        {
            get;
        }

        public ICallableMemberReference Method
        {
            get;
        }

        public override string ToString()
        {
            return IsIntraLinked
                ? $"{VMCall} function_{Function.EntrypointAddress:X4} ({Signature})"
                : $"{VMCall} {Method}";
        }
    }
    
}