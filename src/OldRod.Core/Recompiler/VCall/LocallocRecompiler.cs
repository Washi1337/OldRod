using System.Linq;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Signatures;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.VCall
{
    public class LocallocRecompiler : IVCallRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            var argument = (CilExpression) expression.Arguments.Last().AcceptVisitor(context.Recompiler);
            argument.ExpectedType = context.TargetImage.TypeSystem.UIntPtr;

            return new CilInstructionExpression(CilOpCodes.Localloc, null, argument)
            {
                ExpressionType = new PointerTypeSignature(context.TargetImage.TypeSystem.Byte)
            };
        }
        
    }
}