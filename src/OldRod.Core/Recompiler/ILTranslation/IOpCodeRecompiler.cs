using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.ILTranslation
{
    public interface IOpCodeRecompiler
    {
        CilExpression Translate(RecompilerContext context, ILInstructionExpression expression);
    }
}