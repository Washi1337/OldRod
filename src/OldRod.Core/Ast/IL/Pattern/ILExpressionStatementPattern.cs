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
    public class ILExpressionStatementPattern : ILStatementPattern
    {
        public static implicit operator ILExpressionStatementPattern(ILExpressionPattern expressionPattern)
        {
            return new ILExpressionStatementPattern(expressionPattern);
        }
        
        public ILExpressionStatementPattern(ILExpressionPattern expression)
        {
            Expression = expression;
        }
        
        public ILExpressionPattern Expression
        {
            get;
        }

        public override MatchResult Match(ILAstNode node)
        {
            var result = new MatchResult(false);
            if (node is ILExpressionStatement statement)
            {
                result.Success = true;
                result.CombineWith(Expression.Match(statement.Expression));
            }

            AddCaptureIfNecessary(result, node);
            return result;
        }

        public override string ToString()
        {
            return Expression.ToString();
        }
    }
}