using System;
using System.Collections.Generic;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Recompiler.VCallTranslation
{
    public class BoxRecompiler : IVCallRecompiler
    {
        public IList<CilInstruction> Translate(CompilerContext context, ILVCallExpression expression)
        {
            var result = new List<CilInstruction>();
            
            var boxMetadata = (BoxMetadata) expression.Metadata;
            switch (boxMetadata.ReturnType)
            {
                case VMType.Object:
                    switch (boxMetadata.Value)
                    {
                        case string stringValue:
                            result.Add(CilInstruction.Create(CilOpCodes.Ldstr, stringValue));
                            break;
                    }
                    break;
                case VMType.Byte:
                case VMType.Word:
                case VMType.Dword:
                    result.Add(CilInstruction.Create(CilOpCodes.Ldc_I4, Convert.ToInt32(boxMetadata.Value)));
                    break;
                case VMType.Qword:
                    result.Add(CilInstruction.Create(CilOpCodes.Ldc_I8, Convert.ToInt64(boxMetadata.Value)));
                    break;
                case VMType.Real32:
                    result.Add(CilInstruction.Create(CilOpCodes.Ldc_R4, Convert.ToSingle(boxMetadata.Value)));
                    break;
                case VMType.Real64:
                    result.Add(CilInstruction.Create(CilOpCodes.Ldc_R8, Convert.ToDouble(boxMetadata.Value)));
                    break;
                
                case VMType.Unknown:
                case VMType.Pointer:
                default:
                    throw new NotImplementedException();
            }

            if (boxMetadata.ReturnType != VMType.Object)
                result.Add(CilInstruction.Create(CilOpCodes.Box, boxMetadata.BoxedType));
            
            return result;
        }
    }
}