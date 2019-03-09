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

using System.Collections.Generic;
using System.Linq;
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL.Pattern
{
    public class ILOpCodePattern
    {
        public static implicit operator ILOpCodePattern(ILCode code)
        {
            return new ILOpCodePattern(code);
        } 
        
        public static readonly ILOpCodePattern Any = new ILOpCodeAnyPattern();
        
        private sealed class ILOpCodeAnyPattern : ILOpCodePattern
        {
            public override bool Match(ILCode code)
            {
                return true;
            }

            public override string ToString()
            {
                return "?";
            }
        }
        
        public ILOpCodePattern(params ILCode[] opCode)
        {
            OpCodes = new HashSet<ILCode>(opCode);
        }
        
        public ISet<ILCode> OpCodes
        {
            get;
        }
        
        public virtual bool Match(ILCode code)
        {
            return OpCodes.Contains(code);
        }

        public override string ToString()
        {
            return OpCodes.Count == 1 
                ? OpCodes.First().ToString() 
                : $"({string.Join("|", OpCodes)})";
        }
    }
}