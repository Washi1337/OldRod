using System.Collections.Generic;
using AsmResolver.Net.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.ILTranslation
{
    public class NorRecompiler : IOpCodeRecompiler
    {
        public IList<CilInstruction> Translate(CompilerContext context, ILInstructionExpression expression)
        {
            var result = new List<CilInstruction>();

            // Emit arguments.
            foreach (var argument in expression.Arguments)
                result.AddRange(argument.AcceptVisitor(context.CodeGenerator));

            result.Add(CilInstruction.Create(CilOpCodes.Or));
            result.Add(CilInstruction.Create(CilOpCodes.Not));
            
            return result;
        }
    }
}