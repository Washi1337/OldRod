using System;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.ILTranslation
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
                    return RecompilePushRegister(context, expression);

                case ILCode.PUSHI_DWORD:
                    return new CilInstructionExpression(CilOpCodes.Ldc_I4,
                        unchecked((int) (uint) expression.Operand))
                    {
                        ExpressionType = context.TargetImage.TypeSystem.Int32
                    }.EnsureIsType(context.TargetImage.TypeSystem.UInt32.ToTypeDefOrRef());
                
                case ILCode.PUSHI_QWORD:
                    return new CilInstructionExpression(CilOpCodes.Ldc_I8,
                        unchecked((long) (ulong) expression.Operand))
                    {
                        ExpressionType = context.TargetImage.TypeSystem.Int64
                    }.EnsureIsType(context.TargetImage.TypeSystem.UInt64.ToTypeDefOrRef());

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private CilExpression RecompilePushRegister(RecompilerContext context, ILInstructionExpression expression)
        {
            var valueExpression = expression.Arguments[0];
            var convertedExpression = (CilExpression) valueExpression.AcceptVisitor(context.Recompiler);

            var returnType = expression.ExpressionType.ToMetadataType(context.TargetImage);

            return convertedExpression.EnsureIsType(context.ReferenceImporter.ImportType(returnType.ToTypeDefOrRef()));
        }
    }
}