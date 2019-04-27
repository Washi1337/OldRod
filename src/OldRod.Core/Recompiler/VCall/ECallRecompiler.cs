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
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Annotations;

namespace OldRod.Core.Recompiler.VCall
{
    public class ECallRecompiler : IVCallRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            var ecall = (ECallAnnotation) expression.Annotation;
            var methodSig = (MethodSignature) ecall.Method.Signature;

            // Select calling instruction, return type and call prefix.
            CilInstruction prefix = null;
            TypeSignature resultType;
            CilOpCode opcode;
            switch (ecall.OpCode)
            {
                case VMECallOpCode.CALL:
                    opcode = CilOpCodes.Call;
                    resultType = methodSig.ReturnType;
                    break;
                case VMECallOpCode.CALLVIRT:
                    opcode = CilOpCodes.Callvirt;
                    resultType = methodSig.ReturnType;
                    break;
                case VMECallOpCode.NEWOBJ:
                    opcode = CilOpCodes.Newobj;
                    resultType = ecall.Method.DeclaringType.ToTypeSignature();
                    break;
                case VMECallOpCode.CALLVIRT_CONSTRAINED:
                    prefix = CilInstruction.Create(CilOpCodes.Constrained, ecall.ConstrainedType);
                    opcode = CilOpCodes.Callvirt;
                    resultType = methodSig.ReturnType;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Enter generic context of method.
            context.EnterMember(ecall.Method);

            // Collect arguments.
            var arguments = expression.Arguments
                .Skip(ecall.IsConstrained ? 3 : 2)
                .ToArray();
            
            // Build call expression.
            var result = new CilInstructionExpression(opcode, ecall.Method,
                context.RecompileCallArguments(ecall.Method, arguments, ecall.OpCode == VMECallOpCode.NEWOBJ))
            {
                ExpressionType = resultType.InstantiateGenericTypes(context.GenericContext)
            };

            // Add prefix when necessary.
            if (prefix != null)
            {
                result.Arguments[0].ExpectedType = ecall.ConstrainedType;
                result.Instructions.Insert(0, prefix);
            }

            // Leave generic context.
            context.ExitMember();

            return result;
        }
    }
}