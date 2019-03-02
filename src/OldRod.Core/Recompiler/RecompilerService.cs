using System;
using System.Collections.Generic;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Recompiler.ILTranslation;
using OldRod.Core.Recompiler.VCallTranslation;

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
            // Push
            var push = new PushRecompiler();
            OpCodeRecompilers[ILCode.PUSHR_BYTE] = push;
            OpCodeRecompilers[ILCode.PUSHR_WORD] = push;
            OpCodeRecompilers[ILCode.PUSHR_DWORD] = push;
            OpCodeRecompilers[ILCode.PUSHR_QWORD] = push;
            OpCodeRecompilers[ILCode.PUSHR_OBJECT] = push;
            OpCodeRecompilers[ILCode.PUSHI_DWORD] = push;
            OpCodeRecompilers[ILCode.PUSHI_QWORD] = push;

            // Pop
            OpCodeRecompilers[ILCode.POP] = new PopRecompiler();

            // Add
            var add = new SimpleOpCodeRecompiler(CilOpCodes.Add,
                ILCode.ADD_DWORD, ILCode.ADD_QWORD, ILCode.ADD_R32, ILCode.ADD_R64);
            OpCodeRecompilers[ILCode.ADD_DWORD] = add;
            OpCodeRecompilers[ILCode.ADD_QWORD] = add;
            OpCodeRecompilers[ILCode.ADD_R32] = add;
            OpCodeRecompilers[ILCode.ADD_R64] = add;

            // Cmp
            var cmp = new SimpleOpCodeRecompiler(CilOpCodes.Sub,
                ILCode.CMP, ILCode.CMP_R32, ILCode.CMP_R64, ILCode.CMP_DWORD, ILCode.CMP_QWORD)
            {
                AffectedFlags = VMFlags.OVERFLOW | VMFlags.SIGN | VMFlags.ZERO | VMFlags.CARRY,
                AffectsFlags = true
            };
            OpCodeRecompilers[ILCode.CMP] = cmp;
            OpCodeRecompilers[ILCode.CMP_DWORD] = cmp;
            OpCodeRecompilers[ILCode.CMP_QWORD] = cmp;
            OpCodeRecompilers[ILCode.CMP_R32] = cmp;
            OpCodeRecompilers[ILCode.CMP_R64] = cmp;

            // Nor
            var nor = new NorRecompiler();
            OpCodeRecompilers[ILCode.NOR_DWORD] = nor;
            OpCodeRecompilers[ILCode.NOR_QWORD] = nor;

            // Or
            OpCodeRecompilers[ILCode.__OR_DWORD] = new SimpleOpCodeRecompiler(CilOpCodes.Or, ILCode.__OR_DWORD);

            // Not
            OpCodeRecompilers[ILCode.__NOT_DWORD] = new SimpleOpCodeRecompiler(CilOpCodes.Not, ILCode.__NOT_DWORD);
        }

        private static void SetupVCallRecompilers()
        {
            // Box
            VCallRecompilers[VMCalls.BOX] = new BoxRecompiler();

            // Call
            VCallRecompilers[VMCalls.ECALL] = new ECallRecompiler();
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