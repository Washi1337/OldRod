using System;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Recompiler.VCallTranslation
{
    public class BoxRecompiler : IVCallRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            var boxMetadata = (BoxMetadata) expression.Metadata;
            switch (boxMetadata.ReturnType)
            {
                case VMType.Object:
                    switch (boxMetadata.Value)
                    {
                        case string stringValue:
                            return new CilInstructionExpression(CilOpCodes.Ldstr, stringValue);
                        default:
                            throw new NotImplementedException();
                    }
                case VMType.Byte:
                case VMType.Word:
                case VMType.Dword:
                    return new CilInstructionExpression(CilOpCodes.Ldc_I4, Convert.ToInt32(boxMetadata.Value))
                    {
                        ExpressionType = context.TargetImage.TypeSystem.Int32,    
                    };
                case VMType.Qword:
                    return new CilInstructionExpression(CilOpCodes.Ldc_I8, Convert.ToInt64(boxMetadata.Value))
                    {
                        ExpressionType = context.TargetImage.TypeSystem.Int64,    
                    };
                case VMType.Real32:
                    return new CilInstructionExpression(CilOpCodes.Ldc_R4, Convert.ToSingle(boxMetadata.Value))
                    {
                        ExpressionType = context.TargetImage.TypeSystem.Single,    
                    };
                case VMType.Real64:
                    return new CilInstructionExpression(CilOpCodes.Ldc_R8, Convert.ToDouble(boxMetadata.Value))
                    {
                        ExpressionType = context.TargetImage.TypeSystem.Double,    
                    };

                case VMType.Unknown:
                case VMType.Pointer:
                default:
                    throw new NotImplementedException();
            }

            // TODO: check for boxing or casting.
            
        }
    }
}