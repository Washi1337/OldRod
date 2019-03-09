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
                            return new CilInstructionExpression(CilOpCodes.Ldstr, stringValue)
                            {
                                ExpressionType = context.TargetImage.TypeSystem.String
                            };
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