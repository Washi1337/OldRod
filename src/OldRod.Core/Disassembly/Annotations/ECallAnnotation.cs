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

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.Annotations
{
    public class ECallAnnotation : VCallAnnotation, IMemberProvider
    {
        public ECallAnnotation(IMethodDescriptor method, VMECallOpCode opCode)
            : base(VMCalls.ECALL, method.Signature.ReturnType.ToVMType())
        {
            Method = method;
            OpCode = opCode;
        }
       
        public IMethodDescriptor Method
        {
            get;
        }
        
        IMemberDescriptor IMemberProvider.Member => Method;

        public bool RequiresSpecialAccess
        {
            get
            {
                var methodDef = Method.Resolve();
                if (methodDef is null)
                    return false;
                
                return methodDef.IsPrivate
                       || methodDef.IsFamily
                       || methodDef.IsFamilyAndAssembly
                       || methodDef.IsFamilyOrAssembly
                       || methodDef.DeclaringType.RequiresSpecialAccess();
            }   
        }

        public VMECallOpCode OpCode
        {
            get;
        }

        public bool IsConstrained => OpCode == VMECallOpCode.CALLVIRT_CONSTRAINED;

        public ITypeDefOrRef ConstrainedType
        {
            get;
            set;
        }
        
        public override string ToString()
        {
            return IsConstrained 
                ? $"{OpCode} ({ConstrainedType}) {Method}" 
                : $"{OpCode} {Method}";
        }
    }
}