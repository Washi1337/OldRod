using System;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
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
            var annotation = (TokenAnnotation) expression.Annotation;

            var member = annotation.Member;
            ITypeDescriptor expressionType;

            switch (member.MetadataToken.TokenType)
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
                case MetadataTokenType.MemberRef:
                    var reference = (ICallableMemberReference) member;
                    if (reference.Signature.IsMethod)
                        expressionType = context.ReferenceImporter.ImportType(typeof(RuntimeMethodHandle));
                    else if (reference.Signature.IsField)
                        expressionType = context.ReferenceImporter.ImportType(typeof(RuntimeFieldHandle));
                    else
                        throw new RecompilerException("Detected a reference to a MemberRef that is not a method or a field.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            return new CilInstructionExpression(CilOpCodes.Ldtoken, annotation.Member)
            {
                ExpressionType = expressionType
            };
        }
        
    }
}