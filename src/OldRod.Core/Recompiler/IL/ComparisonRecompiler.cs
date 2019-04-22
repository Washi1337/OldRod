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
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.IL
{
    public class ComparisonRecompiler : IOpCodeRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILInstructionExpression expression)
        {
            CilOpCode opCode;
            switch (expression.OpCode.Code)
            {
                case ILCode.__EQUALS:
                    opCode = CilOpCodes.Ceq;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(expression));
            }

            var result = new CilInstructionExpression(opCode)
            {
                ExpressionType = context.TargetImage.TypeSystem.Boolean
            };

            ITypeDescriptor type = null;
            foreach (var argument in expression.Arguments)
            {
                var cilArgument = (CilExpression) argument.AcceptVisitor(context.Recompiler);
                if (type == null)
                    type = cilArgument.ExpressionType;
                cilArgument.ExpectedType = type;
                result.Arguments.Add(cilArgument);
            }

            return result;
        }
    }
    
}