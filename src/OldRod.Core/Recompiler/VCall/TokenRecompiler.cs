using System;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Metadata;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Recompiler.VCall
{
    public class TokenRecompiler : IVCallRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            var metadata = (TokenAnnotation) expression.Annotation;

            ITypeDescriptor expressionType;
            switch (metadata.Member.MetadataToken.TokenType)
            {
                case MetadataTokenType.TypeDef:
                case MetadataTokenType.TypeRef:
                case MetadataTokenType.TypeSpec:
                    expressionType = context.ReferenceImporter.ImportType(typeof(RuntimeTypeHandle));
                    break;
                case MetadataTokenType.Method:
                case MetadataTokenType.MethodSpec:
                    expressionType = context.ReferenceImporter.ImportType(typeof(RuntimeMethodHandle));
                    break;
                case MetadataTokenType.Field:
                    expressionType = context.ReferenceImporter.ImportType(typeof(RuntimeFieldHandle));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new CilInstructionExpression(CilOpCodes.Ldtoken, metadata.Member)
            {
                ExpressionType = expressionType
            };
        }
        
    }
}