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
            string instructionsString = string.Join(" - ", expression.Instructions.Select(i => i.Operand == null
                ? _formatter.FormatOpCode(i.OpCode)
                : _formatter.FormatOpCode(i.OpCode) + " " + _formatter.FormatOperand(i.OpCode.OperandType, i.Operand)));

            return expression.Arguments.Count == 0
                ? instructionsString
                : $"{instructionsString}({string.Join(", ", expression.Arguments.Select(a=>a.AcceptVisitor(this)))})";
        }
    }
}