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
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.IL
{
    public class PushRecompiler : IOpCodeRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILInstructionExpression expression)
        {
            switch (expression.OpCode.Code)
            {
                case ILCode.PUSHR_OBJECT:
                case ILCode.PUSHR_BYTE:
                case ILCode.PUSHR_WORD:
                case ILCode.PUSHR_DWORD:
                case ILCode.PUSHR_QWORD:
                    var cilExpression = (CilExpression) expression.Arguments[0].AcceptVisitor(context.Recompiler);
                    cilExpression.ExpressionType = expression.OpCode.StackBehaviourPush.GetResultType()
                        .ToMetadataType(context.TargetImage);
                    return cilExpression;

                case ILCode.PUSHI_DWORD:
                    return new CilInstructionExpression(CilOpCodes.Ldc_I4,
                        unchecked((int) (uint) expression.Operand))
                    {
                        ExpressionType = context.TargetImage.TypeSystem.Int32
                    };
                
                case ILCode.PUSHI_QWORD:
                    return new CilInstructionExpression(CilOpCodes.Ldc_I8,
                        unchecked((long) (ulong) expression.Operand))
                    {
                        ExpressionType = context.TargetImage.TypeSystem.Int64
                    };

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}