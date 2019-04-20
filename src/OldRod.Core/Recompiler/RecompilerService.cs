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

using System;
using System.Collections.Generic;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Recompiler.IL;
using OldRod.Core.Recompiler.VCall;

namespace OldRod.Core.Recompiler
{
    public static class RecompilerService
    {
        private static readonly IDictionary<ILCode, IOpCodeRecompiler> OpCodeRecompilers =
            new Dictionary<ILCode, IOpCodeRecompiler>();
        
        private static readonly IDictionary<VMCalls, IVCallRecompiler> VCallRecompilers =
            new Dictionary<VMCalls, IVCallRecompiler>();

        static RecompilerService()
        {
            SetupOpCodeRecompilers();
            SetupVCallRecompilers();
        }

        private static void SetupOpCodeRecompilers()
        {
            var nop = new NopRecompiler();
            OpCodeRecompilers[ILCode.NOP] = nop;
            OpCodeRecompilers[ILCode.TRY] = nop;
            
            // Push
            var push = new PushRecompiler();
            OpCodeRecompilers[ILCode.PUSHR_BYTE] = push;
            OpCodeRecompilers[ILCode.PUSHR_WORD] = push;
            OpCodeRecompilers[ILCode.PUSHR_DWORD] = push;
            OpCodeRecompilers[ILCode.PUSHR_QWORD] = push;
            OpCodeRecompilers[ILCode.PUSHR_OBJECT] = push;
            OpCodeRecompilers[ILCode.PUSHI_DWORD] = push;
            OpCodeRecompilers[ILCode.PUSHI_QWORD] = push;

            // Add
            var add = new SimpleOpCodeRecompiler(CilOpCodes.Add,
                ILCode.ADD_DWORD, ILCode.ADD_QWORD, ILCode.ADD_R32, ILCode.ADD_R64);
            OpCodeRecompilers[ILCode.ADD_DWORD] = add;
            OpCodeRecompilers[ILCode.ADD_QWORD] = add;
            OpCodeRecompilers[ILCode.ADD_R32] = add;
            OpCodeRecompilers[ILCode.ADD_R64] = add;

            // Cmp
            var cmp = new CmpRecompiler();
            OpCodeRecompilers[ILCode.CMP] = cmp;
            OpCodeRecompilers[ILCode.CMP_DWORD] = cmp;
            OpCodeRecompilers[ILCode.CMP_QWORD] = cmp;
            OpCodeRecompilers[ILCode.CMP_R32] = cmp;
            OpCodeRecompilers[ILCode.CMP_R64] = cmp;

            // Nor
            var nor = new SimpleOpCodeRecompiler(new[] { CilOpCodes.Or, CilOpCodes.Not, }, 
                ILCode.NOR_DWORD, ILCode.NOR_QWORD);
            OpCodeRecompilers[ILCode.NOR_DWORD] = nor;
            OpCodeRecompilers[ILCode.NOR_QWORD] = nor;

            // Call
            OpCodeRecompilers[ILCode.CALL] = new CallRecompiler();
            
            // Pseudo opcodes.
            OpCodeRecompilers[ILCode.__SUB_DWORD] = new SimpleOpCodeRecompiler(CilOpCodes.Sub, ILCode.__SUB_DWORD);
            OpCodeRecompilers[ILCode.__OR_DWORD] = new SimpleOpCodeRecompiler(CilOpCodes.Or, ILCode.__OR_DWORD);
            OpCodeRecompilers[ILCode.__AND_DWORD] = new SimpleOpCodeRecompiler(CilOpCodes.And, ILCode.__AND_DWORD);
            OpCodeRecompilers[ILCode.__XOR_DWORD] = new SimpleOpCodeRecompiler(CilOpCodes.Xor, ILCode.__XOR_DWORD);
            OpCodeRecompilers[ILCode.__NOT_DWORD] = new SimpleOpCodeRecompiler(CilOpCodes.Not, ILCode.__NOT_DWORD);
        }

        private static void SetupVCallRecompilers()
        {
            VCallRecompilers[VMCalls.BOX] = new BoxRecompiler();
            VCallRecompilers[VMCalls.CAST] = new CastRecompiler();
            VCallRecompilers[VMCalls.ECALL] = new ECallRecompiler();
            VCallRecompilers[VMCalls.INITOBJ] = new InitObjRecompiler();
            VCallRecompilers[VMCalls.LDFLD] = new LdfldRecompiler();
            VCallRecompilers[VMCalls.LDFTN] = new LdftnRecompiler();
            VCallRecompilers[VMCalls.SIZEOF] = new SizeOfRecompiler();
            VCallRecompilers[VMCalls.STFLD] = new StfldRecompiler();
            VCallRecompilers[VMCalls.THROW] = new ThrowRecompiler();
            VCallRecompilers[VMCalls.TOKEN] = new TokenRecompiler();
            VCallRecompilers[VMCalls.UNBOX] = new UnboxRecompiler();
        }

        public static IOpCodeRecompiler GetOpCodeRecompiler(ILCode code)
        {
            if (!OpCodeRecompilers.TryGetValue(code, out var recompiler))
                throw new NotSupportedException($"Recompilation of opcode {code} is not supported.");
            return recompiler;
        }

        public static IVCallRecompiler GetVCallRecompiler(VMCalls call)
        {
            if (!VCallRecompilers.TryGetValue(call, out var recompiler))
                throw new NotSupportedException($"Recompilation of vcall {call} is not supported.");
            return recompiler;
        }

    }
}