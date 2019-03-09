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
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Recompiler.VCallTranslation
{
    public class ECallRecompiler : IVCallRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            // TODO: check for boxing or casting.

            var ecall = (ECallMetadata) expression.Metadata;
            var methodSig = (MethodSignature) ecall.Method.Signature;

            // Emit calling instruction.
            CilOpCode opcode;
            switch (ecall.OpCode)
            {
                case VMECallOpCode.ECALL_CALL:
                    opcode = CilOpCodes.Call;
                    break;
                case VMECallOpCode.ECALL_CALLVIRT:
                    opcode = CilOpCodes.Callvirt;
                    break;
                case VMECallOpCode.ECALL_NEWOBJ:
                    opcode = CilOpCodes.Newobj;
                    break;
                case VMECallOpCode.ECALL_CALLVIRT_CONSTRAINED:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var result = new CilInstructionExpression(opcode, ecall.Method);

            // Emit arguments.
            for (var i = 0; i < expression.Arguments.Count - 2; i++)
            {
                var cilArgument = (CilExpression) expression.Arguments[i + 2].AcceptVisitor(context.Recompiler);

                var argumentType = methodSig.HasThis
                    ? i == 0
                        ? (ITypeDescriptor) ecall.Method.DeclaringType
                        : methodSig.Parameters[i - 1].ParameterType
                    : methodSig.Parameters[i].ParameterType;

                result.Arguments.Add(
                    cilArgument.EnsureIsType(context.ReferenceImporter.ImportType(argumentType.ToTypeDefOrRef())));
            }

            result.ExpressionType = methodSig.ReturnType;
            return result;
        }
    }
}