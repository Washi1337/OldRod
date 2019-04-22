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
    public class ILVariablePattern : ILExpressionPattern
    {
        public static implicit operator ILVariablePattern(string variableName)
        {
            return new ILVariablePattern(variableName);
        }
        
        public static implicit operator ILVariablePattern(VMRegisters register)
        {
            return new ILVariablePattern(register);
        }
        
        public new static readonly ILVariablePattern Any = new ILVariableAnyPattern();
        
        private sealed class ILVariableAnyPattern : ILVariablePattern
        {
            public ILVariableAnyPattern() 
                : base(null)
            {
            }

            public override MatchResult Match(ILAstNode node)
            {
                var result = new MatchResult(node is ILVariableExpression);
                AddCaptureIfNecessary(result, node);
                return result;
            }

            public override ILAstPattern Capture(string name)
            {
                var result = new ILVariableAnyPattern
                {
                    Captured = true,
                    CaptureName = name
                };
                return result;
            }

            public override string ToString()
            {
                return "?";
            }
        }

        public static readonly ILVariablePattern FL = new ILFLPattern();

        private sealed class ILFLPattern : ILVariablePattern
        {
            public ILFLPattern()
                : base(VMRegisters.FL)
            {
            }
            
            public override MatchResult Match(ILAstNode node)
            {
                var result = new MatchResult(node is ILVariableExpression expression
                                             && expression.Variable is ILFlagsVariable);
                AddCaptureIfNecessary(result, node);
                return result;
            }
        }
        
        public ILVariablePattern(string variableName)
        {
            VariableName = variableName;
        }
        
        public ILVariablePattern(VMRegisters register)
        {
            VariableName = register.ToString();
        }

        public string VariableName
        {
            get;
        }
        
        public override MatchResult Match(ILAstNode node)
        {
            var result = new MatchResult(node is ILVariableExpression expression
                                         && expression.Variable.Name == VariableName);
            AddCaptureIfNecessary(result, node);
            return result;
        }

        public ILVariablePattern CaptureVar(string name)
        {
            return (ILVariablePattern) Capture(name);
        }

        public override string ToString()
        {
            return VariableName;
        }
    }
}