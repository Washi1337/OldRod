using System.Collections.Generic;
using AsmResolver.Net.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.VCallTranslation
{
    public interface IVCallRecompiler
    {
        IList<CilInstruction> Translate(CompilerContext context, ILVCallExpression expression);
    }
}