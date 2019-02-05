using System.Collections.Generic;
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
            var push = new PushRecompiler();
            OpCodeRecompilers[ILCode.PUSHR_BYTE] = push;
            OpCodeRecompilers[ILCode.PUSHR_WORD] = push;
            OpCodeRecompilers[ILCode.PUSHR_DWORD] = push;
            OpCodeRecompilers[ILCode.PUSHR_QWORD] = push;
            OpCodeRecompilers[ILCode.PUSHR_OBJECT] = push;
            OpCodeRecompilers[ILCode.PUSHI_DWORD] = push;
            OpCodeRecompilers[ILCode.PUSHI_QWORD] = push;
            
            var add = new AddRecompiler();
            OpCodeRecompilers[ILCode.ADD_DWORD] = add;
            OpCodeRecompilers[ILCode.ADD_QWORD] = add;
            OpCodeRecompilers[ILCode.ADD_R32] = add;
            OpCodeRecompilers[ILCode.ADD_R64] = add;

            OpCodeRecompilers[ILCode.POP] = new PopRecompiler();
            
            var cmp = new CmpRecompiler();
            OpCodeRecompilers[ILCode.CMP] = cmp;
            OpCodeRecompilers[ILCode.CMP_DWORD] = cmp;
            OpCodeRecompilers[ILCode.CMP_QWORD] = cmp;
            OpCodeRecompilers[ILCode.CMP_R32] = cmp;
            OpCodeRecompilers[ILCode.CMP_R64] = cmp;
            
            var nor = new NorRecompiler();
            OpCodeRecompilers[ILCode.NOR_DWORD] = nor;
            OpCodeRecompilers[ILCode.NOR_QWORD] = nor;
            
            VCallRecompilers[VMCalls.BOX] = new BoxRecompiler();
            VCallRecompilers[VMCalls.ECALL] = new ECallRecompiler();
        }

        public static IOpCodeRecompiler GetOpCodeRecompiler(ILCode code)
        {
            return OpCodeRecompilers[code];
        }

        public static IVCallRecompiler GetVCallRecompiler(VMCalls call)
        {
            return VCallRecompilers[call];
        }
    }
}