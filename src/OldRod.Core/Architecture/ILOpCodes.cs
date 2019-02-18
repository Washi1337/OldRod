namespace OldRod.Core.Architecture
{
    public static class ILOpCodes
    {
        public static readonly ILOpCode[] All = new ILOpCode[256];

        public static readonly ILOpCode NOP = new ILOpCode(ILCode.NOP, (byte) ILOperandType.None
                                                                       | ((byte) ILFlowControl.Next << 8));

        public static readonly ILOpCode LIND_PTR =
            new ILOpCode(ILCode.LIND_PTR, (byte) ILOperandType.None
                                          | ((byte) ILFlowControl.Next << 8)
                                          | ((byte) ILStackBehaviour.PopPtr << 16)
                                          | ((byte) ILStackBehaviour.PushPtr << 24));

        public static readonly ILOpCode LIND_OBJECT =
            new ILOpCode(ILCode.LIND_OBJECT, (byte) ILOperandType.None
                                             | ((byte) ILFlowControl.Next << 8)
                                             | ((byte) ILStackBehaviour.PopPtr << 16)
                                             | ((byte) ILStackBehaviour.PushObject << 24));

        public static readonly ILOpCode LIND_BYTE =
            new ILOpCode(ILCode.LIND_BYTE, (byte) ILOperandType.None
                                           | ((byte) ILFlowControl.Next << 8)
                                           | ((byte) ILStackBehaviour.PopPtr << 16)
                                           | ((byte) ILStackBehaviour.PushByte << 24));

        public static readonly ILOpCode LIND_WORD =
            new ILOpCode(ILCode.LIND_WORD, (byte) ILOperandType.None
                                           | ((byte) ILFlowControl.Next << 8)
                                           | ((byte) ILStackBehaviour.PopPtr << 16)
                                           | ((byte) ILStackBehaviour.PushWord << 24));

        public static readonly ILOpCode LIND_DWORD =
            new ILOpCode(ILCode.LIND_DWORD, (byte) ILOperandType.None
                                            | ((byte) ILFlowControl.Next << 8)
                                            | ((byte) ILStackBehaviour.PopPtr << 16)
                                            | ((byte) ILStackBehaviour.PushDword << 24));

        public static readonly ILOpCode LIND_QWORD = new ILOpCode(ILCode.LIND_QWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopPtr << 16)
            | ((byte) ILStackBehaviour.PushQword << 24));

        public static readonly ILOpCode SIND_PTR =
            new ILOpCode(ILCode.SIND_PTR, (byte) ILOperandType.None
                                          | ((byte) ILFlowControl.Next << 8)
                                          | ((byte) ILStackBehaviour.PopPtr_PopPtr << 16)
                                          | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode SIND_OBJECT = new ILOpCode(ILCode.SIND_OBJECT,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopPtr_PopObject << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode SIND_BYTE = new ILOpCode(ILCode.SIND_BYTE,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopPtr_PopByte << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode SIND_WORD = new ILOpCode(ILCode.SIND_WORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopPtr_PopWord << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode SIND_DWORD = new ILOpCode(ILCode.SIND_DWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopPtr_PopDword << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode SIND_QWORD = new ILOpCode(ILCode.SIND_QWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopPtr_PopQword << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode POP = new ILOpCode(ILCode.POP,
            (byte) ILOperandType.Register
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopAny << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode PUSHR_OBJECT = new ILOpCode(ILCode.PUSHR_OBJECT,
            (byte) ILOperandType.Register
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.None << 16)
            | ((byte) ILStackBehaviour.PushObject << 24));

        public static readonly ILOpCode PUSHR_BYTE = new ILOpCode(ILCode.PUSHR_BYTE,
            (byte) ILOperandType.Register
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.None << 16)
            | ((byte) ILStackBehaviour.PushByte << 24));

        public static readonly ILOpCode PUSHR_WORD = new ILOpCode(ILCode.PUSHR_WORD,
            (byte) ILOperandType.Register
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.None << 16)
            | ((byte) ILStackBehaviour.PushWord << 24));

        public static readonly ILOpCode PUSHR_DWORD = new ILOpCode(ILCode.PUSHR_DWORD,
            (byte) ILOperandType.Register
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.None << 16)
            | ((byte) ILStackBehaviour.PushDword << 24));

        public static readonly ILOpCode PUSHR_QWORD = new ILOpCode(ILCode.PUSHR_QWORD,
            (byte) ILOperandType.Register
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.None << 16)
            | ((byte) ILStackBehaviour.PushQword << 24));

        public static readonly ILOpCode PUSHI_DWORD = new ILOpCode(ILCode.PUSHI_DWORD,
            (byte) ILOperandType.ImmediateDword
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.None << 16)
            | ((byte) ILStackBehaviour.PushDword << 24));

        public static readonly ILOpCode PUSHI_QWORD = new ILOpCode(ILCode.PUSHI_QWORD,
            (byte) ILOperandType.ImmediateQword
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.None << 16)
            | ((byte) ILStackBehaviour.PushQword << 24));

        public static readonly ILOpCode SX_BYTE =
            new ILOpCode(ILCode.SX_BYTE, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopByte << 16)
                                         | ((byte) ILStackBehaviour.PushQword << 24));

        public static readonly ILOpCode SX_WORD =
            new ILOpCode(ILCode.SX_WORD, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopWord << 16)
                                         | ((byte) ILStackBehaviour.PushQword << 24));

        public static readonly ILOpCode SX_DWORD =
            new ILOpCode(ILCode.SX_DWORD, (byte) ILOperandType.None
                                          | ((byte) ILFlowControl.Next << 8)
                                          | ((byte) ILStackBehaviour.PopDword << 16)
                                          | ((byte) ILStackBehaviour.PushQword << 24));

        public static readonly ILOpCode CALL = new ILOpCode(ILCode.CALL,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Call << 8)
            | ((byte) ILStackBehaviour.PopVar << 16)
            | ((byte) ILStackBehaviour.PushVar << 24));

        public static readonly ILOpCode RET = new ILOpCode(ILCode.RET,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Return << 8)
            | ((byte) ILStackBehaviour.None << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode NOR_DWORD =
            new ILOpCode(ILCode.NOR_DWORD, (byte) ILOperandType.None
                                           | ((byte) ILFlowControl.Next << 8)
                                           | ((byte) ILStackBehaviour.PopDword_PopDword << 16)
                                           | ((byte) ILStackBehaviour.PushDword << 24));

        public static readonly ILOpCode NOR_QWORD = new ILOpCode(ILCode.NOR_QWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopQword_PopQword << 16)
            | ((byte) ILStackBehaviour.PushQword << 24));

        public static readonly ILOpCode CMP = new ILOpCode(ILCode.CMP,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopObject_PopObject << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode CMP_DWORD = new ILOpCode(ILCode.CMP_DWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopDword_PopDword << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode CMP_QWORD = new ILOpCode(ILCode.CMP_QWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopQword_PopQword << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode CMP_R32 =
            new ILOpCode(ILCode.CMP_R32, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopReal32_PopReal32 << 16)
                                         | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode CMP_R64 =
            new ILOpCode(ILCode.CMP_R64, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopReal64_PopReal64 << 16)
                                         | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode JZ = new ILOpCode(ILCode.JZ,
            (byte) ILOperandType.None 
            | ((byte) ILFlowControl.ConditionalJump << 8)
                                      | ((byte) ILStackBehaviour.PopQword_PopQword << 16)
                                      | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode JNZ = new ILOpCode(ILCode.JNZ,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.ConditionalJump << 8)
            | ((byte) ILStackBehaviour.PopQword_PopQword << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode JMP = new ILOpCode(ILCode.JMP,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Jump << 8)
            | ((byte) ILStackBehaviour.PopQword << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode SWT = new ILOpCode(ILCode.SWT,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.ConditionalJump << 8)
            | ((byte) ILStackBehaviour.PopQword_PopQword << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode ADD_DWORD = new ILOpCode(ILCode.ADD_DWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopDword_PopDword << 16)
            | ((byte) ILStackBehaviour.PushDword << 24));

        public static readonly ILOpCode ADD_QWORD = new ILOpCode(ILCode.ADD_QWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopQword_PopQword << 16)
            | ((byte) ILStackBehaviour.PushQword << 24));

        public static readonly ILOpCode ADD_R32 =
            new ILOpCode(ILCode.ADD_R32, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopReal32_PopReal32 << 16)
                                         | ((byte) ILStackBehaviour.PushReal32 << 24));

        public static readonly ILOpCode ADD_R64 =
            new ILOpCode(ILCode.ADD_R64, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopReal64_PopReal64 << 16)
                                         | ((byte) ILStackBehaviour.PushReal64 << 24));

        public static readonly ILOpCode SUB_R32 =
            new ILOpCode(ILCode.SUB_R32, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopReal32_PopReal32 << 16)
                                         | ((byte) ILStackBehaviour.PushReal32 << 24));

        public static readonly ILOpCode SUB_R64 =
            new ILOpCode(ILCode.SUB_R64, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopReal64_PopReal64 << 16)
                                         | ((byte) ILStackBehaviour.PushReal64 << 24));

        public static readonly ILOpCode MUL_DWORD = new ILOpCode(ILCode.MUL_DWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopDword_PopDword << 16)
            | ((byte) ILStackBehaviour.PushDword << 24));

        public static readonly ILOpCode MUL_QWORD = new ILOpCode(ILCode.MUL_QWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopQword_PopQword << 16)
            | ((byte) ILStackBehaviour.PushQword << 24));

        public static readonly ILOpCode MUL_R32 =
            new ILOpCode(ILCode.MUL_R32, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopReal32_PopReal32 << 16)
                                         | ((byte) ILStackBehaviour.PushReal32 << 24));

        public static readonly ILOpCode MUL_R64 =
            new ILOpCode(ILCode.MUL_R64, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopReal64_PopReal64 << 16)
                                         | ((byte) ILStackBehaviour.PushReal64 << 24));

        public static readonly ILOpCode DIV_DWORD = new ILOpCode(ILCode.DIV_DWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopDword_PopDword << 16)
            | ((byte) ILStackBehaviour.PushDword << 24));

        public static readonly ILOpCode DIV_QWORD = new ILOpCode(ILCode.DIV_QWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopQword_PopQword << 16)
            | ((byte) ILStackBehaviour.PushQword << 24));

        public static readonly ILOpCode DIV_R32 =
            new ILOpCode(ILCode.DIV_R32, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopReal32_PopReal32 << 16)
                                         | ((byte) ILStackBehaviour.PushReal32 << 24));

        public static readonly ILOpCode DIV_R64 =
            new ILOpCode(ILCode.DIV_R64, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopReal64_PopReal64 << 16)
                                         | ((byte) ILStackBehaviour.PushReal64 << 24));

        public static readonly ILOpCode REM_DWORD = new ILOpCode(ILCode.REM_DWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopDword_PopDword << 16)
            | ((byte) ILStackBehaviour.PushDword << 24));

        public static readonly ILOpCode REM_QWORD = new ILOpCode(ILCode.REM_QWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopQword_PopQword << 16)
            | ((byte) ILStackBehaviour.PushQword << 24));

        public static readonly ILOpCode REM_R32 =
            new ILOpCode(ILCode.REM_R32, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopReal32_PopReal32 << 16)
                                         | ((byte) ILStackBehaviour.PushReal32 << 24));

        public static readonly ILOpCode REM_R64 =
            new ILOpCode(ILCode.REM_R64, (byte) ILOperandType.None
                                         | ((byte) ILFlowControl.Next << 8)
                                         | ((byte) ILStackBehaviour.PopReal64_PopReal64 << 16)
                                         | ((byte) ILStackBehaviour.PushReal64 << 24));

        public static readonly ILOpCode SHR_DWORD = new ILOpCode(ILCode.SHR_DWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopDword_PopDword << 16)
            | ((byte) ILStackBehaviour.PushDword << 24));

        public static readonly ILOpCode SHR_QWORD =
            new ILOpCode(ILCode.SHR_QWORD, (byte) ILOperandType.None
                                           | ((byte) ILFlowControl.Next << 8)
                                           | ((byte) ILStackBehaviour.PopQword_PopQword << 16)
                                           | ((byte) ILStackBehaviour.PushQword << 24));

        public static readonly ILOpCode SHL_DWORD = new ILOpCode(ILCode.SHL_DWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopDword_PopDword << 16)
            | ((byte) ILStackBehaviour.PushDword << 24));

        public static readonly ILOpCode SHL_QWORD = new ILOpCode(ILCode.SHL_QWORD,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopQword_PopQword << 16)
            | ((byte) ILStackBehaviour.PushQword << 24));

        public static readonly ILOpCode FCONV_R32_R64 = new ILOpCode(ILCode.FCONV_R32_R64,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopReal32 << 16)
            | ((byte) ILStackBehaviour.PushReal64 << 24));

        public static readonly ILOpCode FCONV_R64_R32 = new ILOpCode(ILCode.FCONV_R64_R32,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopReal64 << 16)
            | ((byte) ILStackBehaviour.PushReal32 << 24));

        public static readonly ILOpCode FCONV_R32 = new ILOpCode(ILCode.FCONV_R32,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopQword << 16)
            | ((byte) ILStackBehaviour.PushReal32 << 24));

        public static readonly ILOpCode FCONV_R64 = new ILOpCode(ILCode.FCONV_R64,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopQword << 16)
            | ((byte) ILStackBehaviour.PushReal64 << 24));

        public static readonly ILOpCode ICONV_PTR = new ILOpCode(ILCode.ICONV_PTR,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopQword << 16)
            | ((byte) ILStackBehaviour.PushPtr << 24));

        public static readonly ILOpCode ICONV_R64 = new ILOpCode(ILCode.ICONV_R64,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopQword << 16)
            | ((byte) ILStackBehaviour.PushReal64 << 24));

        public static readonly ILOpCode VCALL = new ILOpCode(ILCode.VCALL,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.PopVar << 16)
            | ((byte) ILStackBehaviour.PushVar << 24));

        public static readonly ILOpCode TRY = new ILOpCode(ILCode.TRY,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.None << 16)
            | ((byte) ILStackBehaviour.None << 24));

        public static readonly ILOpCode LEAVE = new ILOpCode(ILCode.LEAVE,
            (byte) ILOperandType.None
            | ((byte) ILFlowControl.Next << 8)
            | ((byte) ILStackBehaviour.None << 16)
            | ((byte) ILStackBehaviour.None << 24));
    }
}