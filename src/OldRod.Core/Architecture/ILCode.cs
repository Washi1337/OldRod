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
    public enum ILCode
    {
        NOP,

        LIND_PTR,
        LIND_OBJECT,
        LIND_BYTE,
        LIND_WORD,
        LIND_DWORD,
        LIND_QWORD,

        SIND_PTR,
        SIND_OBJECT,
        SIND_BYTE,
        SIND_WORD,
        SIND_DWORD,
        SIND_QWORD,

        POP,

        PUSHR_OBJECT,
        PUSHR_BYTE,
        PUSHR_WORD,
        PUSHR_DWORD,
        PUSHR_QWORD,

        PUSHI_DWORD,
        PUSHI_QWORD,

        SX_BYTE,
        SX_WORD,
        SX_DWORD,

        CALL,
        RET,

        NOR_DWORD,
        NOR_QWORD,

        CMP,
        CMP_DWORD,
        CMP_QWORD,
        CMP_R32,
        CMP_R64,

        JZ,
        JNZ,
        JMP,
        SWT,

        ADD_DWORD,
        ADD_QWORD,
        ADD_R32,
        ADD_R64,

        SUB_R32,
        SUB_R64,

        MUL_DWORD,
        MUL_QWORD,
        MUL_R32,
        MUL_R64,

        DIV_DWORD,
        DIV_QWORD,
        DIV_R32,
        DIV_R64,

        REM_DWORD,
        REM_QWORD,
        REM_R32,
        REM_R64,

        SHR_DWORD,
        SHR_QWORD,
        SHL_DWORD,
        SHL_QWORD,

        FCONV_R32_R64,
        FCONV_R64_R32,
        FCONV_R32,
        FCONV_R64,
        ICONV_PTR,
        ICONV_R64,

        VCALL,

        TRY,
        LEAVE,

        Max,
        
        // Pseudo opcodes
        __NOT_DWORD,
        __OR_DWORD,
        __AND_DWORD,
        __XOR_DWORD,
        __SUB_DWORD,
        __PUSH_EXCEPTION,
        __EQUALS_DWORD
    }
}