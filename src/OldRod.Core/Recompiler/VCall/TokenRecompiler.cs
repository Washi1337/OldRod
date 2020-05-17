using System;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Annotations;

namespace OldRod.Core.Recompiler.VCall
{
    public class TokenRecompiler : IVCallRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            var annotation = (TokenAnnotation) expression.Annotation;

            var member = annotation.Member;
            ITypeDescriptor expressionType;

            switch (member.MetadataToken.Table)
            {
                case TableIndex.TypeDef:
                case TableIndex.TypeRef:
                case TableIndex.TypeSpec:
                    expressionType = context.ReferenceImporter.ImportType(typeof(RuntimeTypeHandle));
                    break;
                case TableIndex.Method:
                case TableIndex.MethodSpec:
                    expressionType = context.ReferenceImporter.ImportType(typeof(RuntimeMethodHandle));
                    break;
                case TableIndex.Field:
                    expressionType = context.ReferenceImporter.ImportType(typeof(RuntimeFieldHandle));
                    break;
                case TableIndex.MemberRef:
                    var reference = (MemberReference) member;
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