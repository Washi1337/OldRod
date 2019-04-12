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
using System.Reflection;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
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
                    return CompileRegisterPush(context, expression);

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

        private static CilExpression CompileRegisterPush(RecompilerContext context, ILInstructionExpression expression)
        {
            var cilExpression = (CilExpression) expression.Arguments[0].AcceptVisitor(context.Recompiler);

            var resultType = expression.OpCode.StackBehaviourPush.GetResultType();

            if (cilExpression is CilUnboxToVmExpression)
            {
                // HACK: Unbox expressions unbox the value from the stack, but also convert it to their unsigned
                //       variant and box it again into an object. We need to unpack it again, however, we do not
                //       know the actual type of the value inside the box, as this is determined at runtime.
                //
                //       For now, we just make use of the Convert class provided by .NET, which works but would rather
                //       see a true "native" CIL conversion instead. 
                       
                MethodBase convertMethod;
                switch (resultType)
                {
                    case VMType.Byte:
                        convertMethod = typeof(Convert).GetMethod("ToByte", new[] {typeof(object)});
                        break;
                    case VMType.Word:
                        convertMethod = typeof(Convert).GetMethod("ToUInt16", new[] {typeof(object)});
                        break;
                    case VMType.Dword:
                        convertMethod = typeof(Convert).GetMethod("ToUInt32", new[] {typeof(object)});
                        break;
                    case VMType.Qword:
                        convertMethod = typeof(Convert).GetMethod("ToUInt64", new[] {typeof(object)});
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                cilExpression.ExpectedType = context.TargetImage.TypeSystem.Object;
                cilExpression = new CilInstructionExpression(
                    CilOpCodes.Call,
                    context.ReferenceImporter.ImportMethod(convertMethod),
                    cilExpression);
            }

            cilExpression.ExpressionType = resultType == VMType.Object && !cilExpression.ExpressionType.IsValueType
                ? cilExpression.ExpressionType
                : resultType.ToMetadataType(context.TargetImage);

            return cilExpression;
        }
    }
}