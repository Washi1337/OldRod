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
using AsmResolver.PE.DotNet.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

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
            switch (expression.OpCode.Code)
            {
                case ILCode.__EQUALS_OBJECT:
                    opCode = CilOpCodes.Ceq;
                    break;    
                    
                case ILCode.__EQUALS_R32:
                case ILCode.__EQUALS_R64:
                case ILCode.__EQUALS_DWORD:
                case ILCode.__EQUALS_QWORD:
                    opCode = CilOpCodes.Ceq;
                    break;
                
                case ILCode.__GT_R32:
                case ILCode.__GT_R64:
                    opCode = CilOpCodes.Cgt;
                    break;
                
                case ILCode.__GT_DWORD:
                case ILCode.__GT_QWORD:
                    opCode = CilOpCodes.Cgt_Un;
                    break;
                
                case ILCode.__LT_R32:
                case ILCode.__LT_R64:
                    opCode = CilOpCodes.Clt;
                    break;
                
                case ILCode.__LT_DWORD:
                case ILCode.__LT_QWORD:
                    opCode = CilOpCodes.Clt_Un;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(expression));
            }

            var argumentType = expression.OpCode.StackBehaviourPop.GetArgumentType(0)
                .ToMetadataType(context.TargetModule)
                .ToTypeSignature();
            
            var result = new CilInstructionExpression(opCode)
            {
                ExpressionType = context.TargetModule.CorLibTypeFactory.Boolean
            };

            foreach (var argument in arguments)
            {
                argument.ExpectedType = argumentType;
                result.Arguments.Add(argument);
            }

            return result;
        }
    }
    
}