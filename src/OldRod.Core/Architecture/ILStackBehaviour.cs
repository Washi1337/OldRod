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
    public enum ILStackBehaviour : byte
    {
        None,
        PopAny,
        PopPtr,
        PopByte,
        PopWord,
        PopDword,
        PopQword,
        PopReal32,
        PopReal64,
        PopDword_PopDword,
        PopQword_PopQword,
        PopObject_PopObject,
        PopReal32_PopReal32,
        PopReal64_PopReal64,
        PopPtr_PopPtr,
        PopObject_PopPtr,
        PopByte_PopPtr,
        PopWord_PopPtr,
        PopDword_PopPtr,
        PopQword_PopPtr,
        PopVar,
        PushPtr,
        PushByte,
        PushWord,
        PushDword,
        PushQword,
        PushReal32,
        PushReal64,
        PushObject,
        PushVar,
    }
}