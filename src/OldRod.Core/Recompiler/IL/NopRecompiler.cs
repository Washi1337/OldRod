using AsmResolver.Net.Cil;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.IL
{
    public class NopRecompiler : IOpCodeRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILInstructionExpression expression)
        {
            return new CilInstructionExpression(CilOpCodes.Nop);
        }
    }
}