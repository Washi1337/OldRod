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
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Recompiler.Transform;

namespace OldRod.Core.Recompiler.IL
{
    public class RelationalOpCodeRecompiler : IOpCodeRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILInstructionExpression expression)
        {
            var arguments = expression.Arguments
                .Select(a => (CilExpression) a.AcceptVisitor(context.Recompiler))
                .ToArray();

            CilOpCode opCode;
            TypeSignature argumentType = null;
            switch (expression.OpCode.Code)
            {
                case ILCode.__EQUALS_R32:
                    argumentType = context.TargetImage.TypeSystem.Single;
                    opCode = CilOpCodes.Ceq;
                    break;
                case ILCode.__EQUALS_R64:
                    argumentType = context.TargetImage.TypeSystem.Double;
                    opCode = CilOpCodes.Ceq;
                    break;
                case ILCode.__EQUALS_DWORD:
                    argumentType = context.TargetImage.TypeSystem.UInt32;
                    opCode = CilOpCodes.Ceq;
                    break;
                case ILCode.__EQUALS_QWORD:
                    argumentType = context.TargetImage.TypeSystem.UInt64;
                    opCode = CilOpCodes.Ceq;
                    break;
                case ILCode.__EQUALS_OBJECT:
                    var argumentTypes = arguments
                        .Select(a => a.ExpressionType)
                        .ToArray();
                    
                    argumentType = context.TypeHelper.GetCommonBaseType(argumentTypes)?.ToTypeSignature()
                                   ?? context.TargetImage.TypeSystem.Object;
                    opCode = CilOpCodes.Ceq;
                    break;
                case ILCode.__GT_R32:
                    argumentType = context.TargetImage.TypeSystem.Single;
                    opCode = CilOpCodes.Cgt;
                    break;
                case ILCode.__GT_R64:
                    argumentType = context.TargetImage.TypeSystem.Double;
                    opCode = CilOpCodes.Cgt;
                    break;
                case ILCode.__GT_DWORD:
                    argumentType = context.TargetImage.TypeSystem.UInt32;
                    opCode = CilOpCodes.Cgt_Un;
                    break;
                case ILCode.__GT_QWORD:
                    argumentType = context.TargetImage.TypeSystem.UInt64;
                    opCode = CilOpCodes.Cgt_Un;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(expression));
            }
            
            var result = new CilInstructionExpression(opCode)
            {
                ExpressionType = context.TargetImage.TypeSystem.Boolean
            };
            
            foreach (var argument in arguments)
                result.Arguments.Add(argument);

            return result;
        }
    }
    
}