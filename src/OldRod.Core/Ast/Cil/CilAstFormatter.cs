using System.Linq;
using AsmResolver.Net.Cil;

namespace OldRod.Core.Ast.Cil
{
    public class CilAstFormatter : ICilAstVisitor<string>
    {
        private readonly CilInstructionFormatter _formatter;

        public CilAstFormatter(CilMethodBody methodBody)
        {
            _formatter = new CilInstructionFormatter(methodBody);
        }
        
        public string VisitCompilationUnit(CilCompilationUnit unit)
        {
            throw new System.NotImplementedException();
        }

        public string VisitBlock(CilAstBlock block)
        {
            return string.Join("\\l", block.Statements.Select(x => x.AcceptVisitor(this))) + "\\l";
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