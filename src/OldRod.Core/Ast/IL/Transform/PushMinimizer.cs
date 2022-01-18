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
using OldRod.Core.Ast.IL.Pattern;

namespace OldRod.Core.Ast.IL.Transform
{
    public class PushMinimizer : ChangeAwareILAstTransform
    {
        private static readonly ILInstructionPattern PushPattern =
            ILAstPattern.Instruction(
                    ILCode.PUSHR_BYTE, ILCode.PUSHR_WORD, ILCode.PUSHR_DWORD,
                    ILCode.PUSHR_QWORD, ILCode.PUSHR_OBJECT)
                .WithAnyOperand()
                .WithArguments(ILExpressionPattern.Any.CaptureExpr("expr"));
        
        public override string Name => "Push Minimizer";

        public override bool VisitInstructionExpression(ILInstructionExpression expression)
        {
            // Any push expression that pushes the same type as its argument is superfluous.
            
            bool changed = base.VisitInstructionExpression(expression);

            var match = PushPattern.Match(expression);
            if (match.Success)
            {
                var expr = (ILExpression) match.Captures["expr"][0];
                if (expression.ExpressionType == expr.ExpressionType)
                {
                    expression.ReplaceWith(expr.Remove());
                    changed = true;
                }
            }

            return changed;
        }
        
    }
}