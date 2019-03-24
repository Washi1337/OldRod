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

namespace OldRod.Core.Disassembly.Inference
{
    public struct FunctionReference
    {
        public FunctionReference(VMFunction caller, int offset, FunctionReferenceType referenceType, VMFunction callee)
        {
            Caller = caller;
            Offset = offset;
            Callee = callee;
            ReferenceType = referenceType;
        }
        
        public VMFunction Caller
        {
            get;
        }

        public int Offset
        {
            get;
        }

        public FunctionReferenceType ReferenceType
        {
            get;
        }

        public VMFunction Callee
        {
            get;
        }

        public override string ToString()
        {
            return $"<{Caller}> IL_{Offset:X4} {ReferenceType} {Callee}";
        }
    }
}