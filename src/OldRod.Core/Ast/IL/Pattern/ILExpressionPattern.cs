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
    public abstract class ILExpressionPattern : ILAstPattern
    {
        public static readonly ILExpressionPattern Any = new ILExpressionAnyPattern();
        
        private sealed class ILExpressionAnyPattern : ILExpressionPattern
        {
            public override MatchResult Match(ILAstNode node)
            {
                var result = new MatchResult(node is ILExpression);
                AddCaptureIfNecessary(result, node);
                return result;
            }

            public override ILAstPattern Capture(string name)
            {
                return new ILExpressionAnyPattern
                {
                    Captured = true,
                    CaptureName = name
                };
            }

            public override string ToString()
            {
                return "?";
            }
        }

        public ILExpressionPattern CaptureExpr(string name)
        {
            return (ILExpressionPattern) Capture(name);
        }
    }
}