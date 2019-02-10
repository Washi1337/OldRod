using System.Collections.Generic;
using AsmResolver.Net.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.ILTranslation
{
    public interface IOpCodeRecompiler
    {
        IList<CilInstruction> Translate(CompilerContext context, ILInstructionExpression expression);
    }
}