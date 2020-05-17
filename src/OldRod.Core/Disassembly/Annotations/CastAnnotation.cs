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

using AsmResolver.DotNet;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.Annotations
{
    public class CastAnnotation : TypeAnnotation
    {
        public CastAnnotation(ITypeDefOrRef type, bool isSafeCast) 
            : base(VMCalls.CAST, type)
        {
            IsSafeCast = isSafeCast;
        }

        public bool IsSafeCast
        {
            get;
        }

        public override string ToString()
        {
            return $"{VMCall} {(IsSafeCast ? "safe " : "")}{Type}";
        }
    }
}