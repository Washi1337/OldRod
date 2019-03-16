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

using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.DataFlow
{
    public class EHFrame
    {
        public ulong TryStart
        {
            get;
            set;
        }
        
        public EHType Type
        {
            get;
            set;
        }

        public ITypeDefOrRef CatchType
        {
            get;
            set;
        }

        public ulong FilterAddress
        {
            get;
            set;
        }

        public ulong HandlerAddress
        {
            get;
            set;
        }

        public override string ToString()
        {
            return $"{Type} (try: {TryStart:X4}, handler: {HandlerAddress:X4})";
        }
        
    }
}