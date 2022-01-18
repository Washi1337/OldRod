using System;
using AsmResolver.PE.DotNet.Cil;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Annotations;

namespace OldRod.Core.Recompiler.VCall
{
    public class ThrowRecompiler : IVCallRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            var annotation = (ThrowAnnotation) expression.Annotation;

            if (annotation.IsRethrow)
                return new CilInstructionExpression(CilOpCodes.Rethrow);

            var argument = (CilExpression) expression.Arguments[2].AcceptVisitor(context.Recompiler);
            argument.ExpectedType = context.ReferenceImporter.ImportType(typeof(Exception));
            
            var result = new CilInstructionExpression(CilOpCodes.Throw,
                null,
                argument);

            return result;
        }
    }
}