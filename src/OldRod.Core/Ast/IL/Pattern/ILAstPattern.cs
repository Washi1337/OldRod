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

using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL.Pattern
{
    public abstract class ILAstPattern
    {
        public bool Captured
        {
            get;
            protected set;
        }

        public string CaptureName
        {
            get;
            protected set;
        }

        public abstract MatchResult Match(ILAstNode node);

        public virtual ILAstPattern Capture(string name)
        {
            Captured = true;
            CaptureName = name;
            return this;
        }

        protected void AddCaptureIfNecessary(MatchResult result, ILAstNode node)
        {
            if (result.Success && Captured)
                result.AddCapture(CaptureName, node);
        }

        public static ILSequencePattern<T> Sequence<T>(params ILAstPattern[] sequence) 
            where T : ILAstNode
        {
            return new ILSequencePattern<T>(sequence);
        }

        public static ILAssignmentPattern Assignment(VMRegisters variable, ILExpressionPattern value)
        {
            return new ILAssignmentPattern(variable, value);
        }

        public static ILExpressionStatementPattern Expression(ILExpressionPattern pattern)
        {
            return new ILExpressionStatementPattern(pattern);
        }
        
        public static ILAssignmentPattern Assignment(ILVariablePattern variable, ILExpressionPattern value)
        {
            return new ILAssignmentPattern(variable, value);
        }

        public static ILInstructionPattern Instruction(params ILCode[] opCodes)
        {
            return new ILInstructionPattern(new ILOpCodePattern(opCodes));
        }

        public static ILInstructionPattern PushDwordReg(VMRegisters register)
        {
            return Instruction(ILCode.PUSHR_DWORD)
                .WithOperand(register)
                .WithArguments(new ILVariablePattern(register));
        }

        public static ILInstructionPattern PushAnyObjectReg()
        {
            return Instruction(ILCode.PUSHR_OBJECT)
                .WithAnyOperand()
                .WithArguments(ILVariablePattern.Any);
        }

        public static ILInstructionPattern PushAnyDword()
        {
            return Instruction(ILCode.PUSHI_DWORD)
                .WithAnyOperand();
        }

    }
}