using System.Collections.Generic;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.ILTranslation
{
    public class CmpRecompiler : IOpCodeRecompiler
    {
        public IList<CilInstruction> Translate(CompilerContext context, ILInstructionExpression expression)
        {
            var result = new List<CilInstruction>();
            
            result.AddRange(context.BuildBinaryExpression(
                expression.Arguments[0].AcceptVisitor(context.CodeGenerator),
                expression.Arguments[1].AcceptVisitor(context.CodeGenerator),
                new[] {CilInstruction.Create(CilOpCodes.Sub) },
                context.Constants.GetFlagMask(VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY)));
            
            return result;
        }
    }
}