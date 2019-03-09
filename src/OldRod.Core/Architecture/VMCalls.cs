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

namespace OldRod.Core.Architecture
{
    public enum VMCalls
    {
        EXIT = 0,
        BREAK = 1,
        ECALL = 2,
        CAST = 3,
        CKFINITE = 4,
        CKOVERFLOW = 5,
        RANGECHK = 6,
        INITOBJ = 7,
        LDFLD = 8,
        LDFTN = 9,
        TOKEN = 10,
        THROW = 11,
        SIZEOF = 12,
        STFLD = 13,
        BOX = 14,
        UNBOX = 15,
        LOCALLOC = 16,
    
        Max
    }
}