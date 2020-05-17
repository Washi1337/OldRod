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

using System;
using System.Linq;
using System.Text;
using AsmResolver.DotNet.Code.Cil;

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
            const int maxLineLength = 100;
            var builder = new StringBuilder();

            foreach (var value in block.Statements)
            {
                string stringValue = value.AcceptVisitor(this);

                for (int i = 0; i < stringValue.Length; i += maxLineLength)
                {
                    int lineLength = Math.Min(stringValue.Length - i, maxLineLength);
                    string line = stringValue.Substring(i, lineLength);
                    if (i > 0)
                        builder.Append("     ");
                    builder.Append(line);
                    builder.Append("\\l");
                }
            }
            
            return builder.ToString();
        }

        public string VisitExpressionStatement(CilExpressionStatement statement)
        {
            return statement.Expression.AcceptVisitor(this);
        }

        public string VisitAssignmentStatement(CilAssignmentStatement statement)
        {
            return $"stloc {statement.Variable.Name} ({statement.Value.AcceptVisitor(this)})";
        }

        public string VisitInstructionExpression(CilInstructionExpression expression)
        {
            string instructionsString =
                (expression.ShouldEmitFlagsUpdate ? "fl_" : "")
                + string.Join(" - ", expression.Instructions.Select(i => i.Operand == null
                    ? _formatter.FormatOpCode(i.OpCode)
                    : _formatter.FormatOpCode(i.OpCode) + " " +
                      _formatter.FormatOperand(i.OpCode.OperandType, i.Operand)));

            return expression.Arguments.Count == 0
                ? instructionsString
                : $"{instructionsString}({string.Join(", ", expression.Arguments.Select(a=>a.AcceptVisitor(this)))})";
        }

        public string VisitUnboxToVmExpression(CilUnboxToVmExpression expression)
        {
            return $"unbox.tovm({expression.Type})({expression.Expression.AcceptVisitor(this)})";
        }

        public string VisitVariableExpression(CilVariableExpression expression)
        {
            return $"ldloc {expression.Variable.Name}";
        }
     
    }
}