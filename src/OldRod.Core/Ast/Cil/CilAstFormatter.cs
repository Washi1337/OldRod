// Project OldRod - A KoiVM devirtualisation utility.
// Copyright (C) 2019 Washi
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

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