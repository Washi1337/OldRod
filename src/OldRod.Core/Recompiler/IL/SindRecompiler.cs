using System;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.IL
{
    public class SindRecompiler : IOpCodeRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILInstructionExpression expression)
        {
            CilOpCode code;
            switch (expression.OpCode.Code)
            {
                case ILCode.SIND_PTR:
                    code = CilOpCodes.Stind_I;
                    break;
                case ILCode.SIND_BYTE:
                    code = CilOpCodes.Stind_I1;
                    break;
                case ILCode.SIND_WORD:
                    code = CilOpCodes.Stind_I2;
                    break;
                case ILCode.SIND_DWORD:
                    code = CilOpCodes.Stind_I4;
                    break;
                case ILCode.SIND_QWORD:
                    code = CilOpCodes.Stind_I8;
                    break;
                case ILCode.SIND_OBJECT:
                    code = CilOpCodes.Stind_Ref;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(expression));
            }

            var value = (CilExpression) expression.Arguments[0].AcceptVisitor(context.Recompiler);
            var destination = (CilExpression) expression.Arguments[1].AcceptVisitor(context.Recompiler);

            switch (destination.ExpressionType)
            {
                case PointerTypeSignature pointerType:
                    value.ExpectedType = pointerType.BaseType;
                    break;
                case ByReferenceTypeSignature byRefType:
                    value.ExpectedType = byRefType.BaseType;
                    break;
            }


            return new CilInstructionExpression(code, null, destination, value)
            {
                ExpressionType = null
            };
        }
        
    }
}