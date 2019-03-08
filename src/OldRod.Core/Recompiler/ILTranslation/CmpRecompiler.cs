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

using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.ILTranslation
{
    public class CmpRecompiler : SimpleOpCodeRecompiler
    {
        public CmpRecompiler()
            : base(CilOpCodes.Sub, 
                ILCode.CMP, ILCode.CMP_R32, 
                ILCode.CMP_R64, ILCode.CMP_DWORD, 
                ILCode.CMP_QWORD)
        {
            AffectedFlags = VMFlags.OVERFLOW | VMFlags.SIGN | VMFlags.ZERO | VMFlags.CARRY;
            AffectsFlags = true;
            InvertedFlagsUpdate = true;
        }

        public override CilExpression Translate(RecompilerContext context, ILInstructionExpression expression)
        {
            var result = base.Translate(context, expression);
            result.ExpressionType = null;
            return result;
        }
    }
}