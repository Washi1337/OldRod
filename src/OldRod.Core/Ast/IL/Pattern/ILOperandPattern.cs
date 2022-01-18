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

namespace OldRod.Core.Ast.IL.Pattern
{
    public class ILOperandPattern
    {
        public static readonly ILOperandPattern Any = new ILOperandAnyPattern();
        
        public static readonly ILOperandPattern Null = new ILOperandPattern(default(object));
        
        private sealed class ILOperandAnyPattern : ILOperandPattern
        {
            public ILOperandAnyPattern() 
                : base(default(object))
            {
            }
            
            public override bool Match(object operand)
            {
                return true;
            }

            public override string ToString()
            {
                return "?";
            }
        }
        
        public ILOperandPattern(object operand)
        {
            Operands = new List<object> { operand };
        }
        
        public ILOperandPattern(params object[] operands)
        {
            Operands = new List<object>(operands);
        }
        
        public ILOperandPattern(IEnumerable<object> operands)
        {
            Operands = new List<object>(operands);
        }

        public IList<object> Operands
        {
            get;
        }

        public virtual bool Match(object operand)
        {
            return Operands.Contains(operand);
        }

        public override string ToString()
        {
            return $"{{{string.Join(", ", Operands.Select(o => o is null ? "null" : o.ToString()))}}}";
        }
    }
}