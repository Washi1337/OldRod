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
using System.Collections.Generic;
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL.Pattern
{
    public class ILInstructionPattern : ILExpressionPattern
    {
        public ILInstructionPattern(ILOpCodePattern opCode)
        {
            OpCode = opCode ?? throw new ArgumentNullException(nameof(opCode));
            Operand = ILOperandPattern.Null;
            Arguments = new List<ILExpressionPattern>();
        }

        public ILInstructionPattern(ILOpCodePattern opCode, ILOperandPattern operand,
            params ILExpressionPattern[] arguments)
        {
            OpCode = opCode ?? throw new ArgumentNullException(nameof(opCode));
            Operand = operand;
            Arguments = new List<ILExpressionPattern>(arguments);
        }

        public ILOpCodePattern OpCode
        {
            get;
            set;
        }

        public ILOperandPattern Operand
        {
            get;
            set;
        }

        public IList<ILExpressionPattern> Arguments
        {
            get;
        }

        public override MatchResult Match(ILAstNode node)
        {
            var result = new MatchResult(false);

            if (node is ILInstructionExpression expression)
            {
                result.Success = OpCode.Match(expression.OpCode.Code)
                                 && Operand.Match(expression.Operand)
                                 && expression.Arguments.Count == Arguments.Count;

                for (int i = 0; result.Success && i < expression.Arguments.Count; i++)
                {
                    var argumentMatch = Arguments[i].Match(expression.Arguments[i]);
                    result.CombineWith(argumentMatch);
                }
            }

            AddCaptureIfNecessary(result, node);
            return result;
        }

        public new ILInstructionPattern Capture(string name)
        {
            return (ILInstructionPattern) base.Capture(name);
        }

        public ILInstructionPattern WithOpCode(params ILCode[] opCodes)
        {
            OpCode = new ILOpCodePattern(opCodes);
            return this;
        }

        public ILInstructionPattern WithOpCode(ILOpCodePattern opCode)
        {
            OpCode = opCode;
            return this;
        }

        public ILInstructionPattern WithOperand(ILOperandPattern operand)
        {
            Operand = operand;
            return this;
        }

        public ILInstructionPattern WithAnyOperand()
        {
            Operand = ILOperandPattern.Any;
            return this;
        }

        public ILInstructionPattern WithOperand(params object[] operands)
        {
            Operand = new ILOperandPattern(operands);
            return this;
        }

        public ILInstructionPattern WithArguments(params ILExpressionPattern[] arguments)
        {
            Arguments.Clear();
            foreach (var arg in arguments)
                Arguments.Add(arg);
            return this;
        }

        public override string ToString()
        {
            if (Operand is { Operands.Count: 0 })
                return $"{OpCode}({string.Join(", ", Arguments)})";
            if (Arguments.Count == 0)
                return OpCode + "(" + Operand + ")";
            return $"{OpCode}({Operand} : {string.Join(", ", Arguments)})";
        }
    }
}