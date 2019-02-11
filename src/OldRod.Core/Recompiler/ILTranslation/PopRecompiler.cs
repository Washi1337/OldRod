using System.Linq;
using AsmResolver.Net.Cil;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.ILTranslation
{
    public class PopRecompiler : IOpCodeRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILInstructionExpression expression)
        {
            var variableEntry = context.Variables.First(x => x.Key.Name == expression.Operand.ToString());
            var ilVariable = variableEntry.Key;
            var cilVariable = variableEntry.Value;

            var result = new CilInstructionExpression(CilOpCodes.Stloc, cilVariable);
         
            var argument = expression.Arguments[0];
            result.Arguments.Add((CilExpression) argument.AcceptVisitor(context.Recompiler));
            
            // TODO: Check for boxing or casting.
            
            return result;
        }
    }
}