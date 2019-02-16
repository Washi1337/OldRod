using System.Linq;
using AsmResolver.Net.Cil;
using OldRod.Core.Ast.Cil;

namespace OldRod.Pipeline
{
    internal class CilAstBlockFormatter : ICilAstVisitor<string>
    {
        private readonly CilInstructionFormatter _formatter;

        public CilAstBlockFormatter(CilMethodBody methodBody)
        {
            _formatter = new CilInstructionFormatter(methodBody);
        }
        
        public string VisitCompilationUnit(CilCompilationUnit unit)
        {
            throw new System.NotImplementedException();
        }

        public string VisitBlock(CilAstBlock block)
        {
            return string.Join("|", block.Statements.Select(x => x.AcceptVisitor(this)));
        }

        public string VisitExpressionStatement(CilExpressionStatement statement)
        {
            return statement.Expression.AcceptVisitor(this);
        }

        public string VisitInstructionExpression(CilInstructionExpression expression)
        {
            string formattedOpCode = _formatter.FormatOpCode(expression.OpCode);
            string formattedArguments = string.Join(", ", expression.Arguments.Select(x => x.AcceptVisitor(this)));

            if (expression.Operand == null)
                return $"{formattedOpCode}({formattedArguments})";

            string formattedOperand = _formatter.FormatOperand(expression.OpCode.OperandType, expression.Operand);
            return expression.Arguments.Count == 0
                ? $"{formattedOpCode}({formattedOperand})"
                : $"{formattedOpCode}({formattedOperand} : {formattedArguments})";
        }
    }
}