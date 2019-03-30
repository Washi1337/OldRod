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

using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.Cil
{
    public abstract class CilExpression : CilAstNode
    {
        private static readonly SignatureComparer Comparer = new SignatureComparer();
        
        public ITypeDescriptor ExpressionType
        {
            get;
            set;
        }

        public ITypeDescriptor ExpectedType
        {
            get;
            set;
        }

        public VMFlags AffectedFlags
        {
            get;
            set;
        }

        public bool ShouldEmitFlagsUpdate
        {
            get;
            set;
        }
        
        public bool InvertedFlagsUpdate
        {
            get;
            set;
        }
      
    }
}