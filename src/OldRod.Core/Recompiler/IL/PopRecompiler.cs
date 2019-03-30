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
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.IL
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
            return result.EnsureIsType(context.ReferenceImporter.ImportType(cilVariable.Signature.VariableType.ToTypeDefOrRef()));
        }
    }
}