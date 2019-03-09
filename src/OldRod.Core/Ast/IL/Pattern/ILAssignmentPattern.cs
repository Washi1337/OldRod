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

namespace OldRod.Core.Ast.IL.Pattern
{
    public class ILAssignmentPattern : ILStatementPattern
    {
        public ILAssignmentPattern(ILVariablePattern variable, ILExpressionPattern value)
        {
            Variable = variable;
            Value = value;
        }
        
        public ILVariablePattern Variable
        {
            get;
        }

        public ILExpressionPattern Value
        {
            get;
        }
        
        public override MatchResult Match(ILAstNode node)
        {
            var result = new MatchResult(false);

            if (node is ILAssignmentStatement statement)
            {
                result.Success = Variable.VariableName == null || Variable.VariableName == statement.Variable.Name;
                if (result.Success) 
                    result.CombineWith(Value.Match(statement.Value));
            }

            AddCaptureIfNecessary(result, node);
            return result;
        }

        public override string ToString()
        {
            return $"{Variable} = {Value}";
        }
    }
}