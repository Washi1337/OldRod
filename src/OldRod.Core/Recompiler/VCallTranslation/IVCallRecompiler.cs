using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.VCallTranslation
{
    public interface IVCallRecompiler
    {
        CilExpression Translate(RecompilerContext context, ILVCallExpression expression);
    }
}