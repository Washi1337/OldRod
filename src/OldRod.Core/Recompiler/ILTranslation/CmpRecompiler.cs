using System.Collections.Generic;
using AsmResolver.Net.Cil;
using OldRod.Core.Ast;

namespace OldRod.Core.Recompiler.ILTranslation
{
    public class CmpRecompiler : IOpCodeRecompiler
    {
        public IList<CilInstruction> Translate(CompilerContext context, ILInstructionExpression expression)
        {
            var result = new List<CilInstruction>();

            // Emit arguments.
            foreach (var argument in expression.Arguments)
                result.AddRange(argument.AcceptVisitor(context.CodeGenerator));

            result.Add(CilInstruction.Create(CilOpCodes.Sub));
            result.Add(CilInstruction.Create(CilOpCodes.Pop)); // TODO: set FL
            
            return result;
        }
    }
}