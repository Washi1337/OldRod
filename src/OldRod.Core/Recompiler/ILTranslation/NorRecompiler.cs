using AsmResolver.Net.Cil;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.ILTranslation
{
    public class NorRecompiler : IOpCodeRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILInstructionExpression expression)
        {
            var or = new CilInstructionExpression(CilOpCodes.Or);

            // Emit arguments.
            foreach (var argument in expression.Arguments)
                or.Arguments.Add((CilExpression) argument.AcceptVisitor(context.Recompiler));

            return new CilInstructionExpression(CilOpCodes.Not, null, or);
        }
    }
}