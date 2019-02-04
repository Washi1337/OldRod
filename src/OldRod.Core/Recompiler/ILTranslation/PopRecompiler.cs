using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast;

namespace OldRod.Core.Recompiler.ILTranslation
{
    public class PopRecompiler : IOpCodeRecompiler
    {
        public IList<CilInstruction> Translate(CompilerContext context, ILInstructionExpression expression)
        {
            var result = new List<CilInstruction>();
            var argument = expression.Arguments[0];
            result.AddRange(argument.AcceptVisitor(context.CodeGenerator));

            var variableEntry = context.Variables.First(x => x.Key.Name == expression.Operand.ToString());
            var ilVariable = variableEntry.Key;
            var cilVariable = variableEntry.Value;

            if (ilVariable.VariableType == VMType.Object && expression.ExpressionType != VMType.Object)
            {
                result.Add(CilInstruction.Create(CilOpCodes.Box,
                    context.ReferenceImporter.ImportType(cilVariable.VariableType.ToTypeDefOrRef())));
            }

            result.Add(CilInstruction.Create(CilOpCodes.Stloc, cilVariable));
            
            return result;
        }
    }
}