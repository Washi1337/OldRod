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

using static OldRod.Core.Architecture.ILOpCode;

namespace OldRod.Core.Architecture
{
    public static class ILOpCodes
    {
        public static readonly ILOpCode[] All = new ILOpCode[256];

        public static readonly ILOpCode NOP = new ILOpCode(ILCode.NOP,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset));

        public static readonly ILOpCode LIND_PTR = new ILOpCode(ILCode.LIND_PTR,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopPtr << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushPtr << StackBehaviourPushOffset));

        public static readonly ILOpCode LIND_OBJECT = new ILOpCode(ILCode.LIND_OBJECT,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopPtr << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushObject << StackBehaviourPushOffset));

        public static readonly ILOpCode LIND_BYTE = new ILOpCode(ILCode.LIND_BYTE,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopPtr << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushByte << StackBehaviourPushOffset));

        public static readonly ILOpCode LIND_WORD = new ILOpCode(ILCode.LIND_WORD,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopPtr << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushWord << StackBehaviourPushOffset));

        public static readonly ILOpCode LIND_DWORD = new ILOpCode(ILCode.LIND_DWORD,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopPtr << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));

        public static readonly ILOpCode LIND_QWORD = new ILOpCode(ILCode.LIND_QWORD,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopPtr << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushQword << StackBehaviourPushOffset));

        public static readonly ILOpCode SIND_PTR = new ILOpCode(ILCode.SIND_PTR,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopPtr_PopPtr << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode SIND_OBJECT = new ILOpCode(ILCode.SIND_OBJECT,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopPtr_PopObject << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode SIND_BYTE = new ILOpCode(ILCode.SIND_BYTE,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopPtr_PopByte << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode SIND_WORD = new ILOpCode(ILCode.SIND_WORD,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopPtr_PopWord << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode SIND_DWORD = new ILOpCode(ILCode.SIND_DWORD,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopPtr_PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode SIND_QWORD = new ILOpCode(ILCode.SIND_QWORD,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopPtr_PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode POP = new ILOpCode(ILCode.POP,
            ((byte) ILOperandType.Register << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopAny << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode PUSHR_OBJECT = new ILOpCode(ILCode.PUSHR_OBJECT,
            ((byte) ILOperandType.Register << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushObject << StackBehaviourPushOffset));

        public static readonly ILOpCode PUSHR_BYTE = new ILOpCode(ILCode.PUSHR_BYTE,
            ((byte) ILOperandType.Register << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushByte << StackBehaviourPushOffset));

        public static readonly ILOpCode PUSHR_WORD = new ILOpCode(ILCode.PUSHR_WORD,
            ((byte) ILOperandType.Register << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushWord << StackBehaviourPushOffset));

        public static readonly ILOpCode PUSHR_DWORD = new ILOpCode(ILCode.PUSHR_DWORD,
            ((byte) ILOperandType.Register << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));

        public static readonly ILOpCode PUSHR_QWORD = new ILOpCode(ILCode.PUSHR_QWORD,
            ((byte) ILOperandType.Register << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushQword << StackBehaviourPushOffset));

        public static readonly ILOpCode PUSHI_DWORD = new ILOpCode(ILCode.PUSHI_DWORD,
            ((byte) ILOperandType.ImmediateDword << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));

        public static readonly ILOpCode PUSHI_QWORD = new ILOpCode(ILCode.PUSHI_QWORD,
            ((byte) ILOperandType.ImmediateQword << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushQword << StackBehaviourPushOffset));

        public static readonly ILOpCode SX_BYTE = new ILOpCode(ILCode.SX_BYTE,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopByte << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushQword << StackBehaviourPushOffset));

        public static readonly ILOpCode SX_WORD = new ILOpCode(ILCode.SX_WORD,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopWord << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushQword << StackBehaviourPushOffset));

        public static readonly ILOpCode SX_DWORD = new ILOpCode(ILCode.SX_DWORD,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushQword << StackBehaviourPushOffset));

        public static readonly ILOpCode CALL = new ILOpCode(ILCode.CALL,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Call << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopVar << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushVar << StackBehaviourPushOffset));

        public static readonly ILOpCode RET = new ILOpCode(ILCode.RET,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Return << FlowControlOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode NOR_DWORD = new ILOpCode(ILCode.NOR_DWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword_PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));
        
        public static readonly ILOpCode NOR_QWORD = new ILOpCode(ILCode.NOR_QWORD,
            ((byte)(VMFlags.ZERO | VMFlags.SIGN) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword_PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushQword << StackBehaviourPushOffset));

        public static readonly ILOpCode CMP = new ILOpCode(ILCode.CMP,
            ((byte)(VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopObject_PopObject << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode CMP_DWORD = new ILOpCode(ILCode.CMP_DWORD,
            ((byte)(VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword_PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode CMP_QWORD = new ILOpCode(ILCode.CMP_QWORD,
            ((byte)(VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword_PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode CMP_R32 = new ILOpCode(ILCode.CMP_R32,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal32_PopReal32 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode CMP_R64 = new ILOpCode(ILCode.CMP_R64,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal64_PopReal64 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode JZ = new ILOpCode(ILCode.JZ,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.ConditionalJump << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword_PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode JNZ = new ILOpCode(ILCode.JNZ,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.ConditionalJump << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword_PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode JMP = new ILOpCode(ILCode.JMP,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Jump << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode SWT = new ILOpCode(ILCode.SWT,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.ConditionalJump << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword_PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode ADD_DWORD = new ILOpCode(ILCode.ADD_DWORD,
            ((byte)(VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword_PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));

        public static readonly ILOpCode ADD_QWORD = new ILOpCode(ILCode.ADD_QWORD,
            ((byte)(VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword_PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushQword << StackBehaviourPushOffset));

        public static readonly ILOpCode ADD_R32 = new ILOpCode(ILCode.ADD_R32,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal32_PopReal32 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal32 << StackBehaviourPushOffset));

        public static readonly ILOpCode ADD_R64 = new ILOpCode(ILCode.ADD_R64,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal64_PopReal64 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal64 << StackBehaviourPushOffset));

        public static readonly ILOpCode SUB_R32 = new ILOpCode(ILCode.SUB_R32,
            ((byte)(VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal32_PopReal32 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal32 << StackBehaviourPushOffset));

        public static readonly ILOpCode SUB_R64 = new ILOpCode(ILCode.SUB_R64,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal64_PopReal64 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal64 << StackBehaviourPushOffset));

        public static readonly ILOpCode MUL_DWORD = new ILOpCode(ILCode.MUL_DWORD,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword_PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));

        public static readonly ILOpCode MUL_QWORD = new ILOpCode(ILCode.MUL_QWORD,
            ((byte)(VMFlags.ZERO | VMFlags.SIGN | VMFlags.UNSIGNED) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword_PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushQword << StackBehaviourPushOffset));

        public static readonly ILOpCode MUL_R32 = new ILOpCode(ILCode.MUL_R32,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.UNSIGNED) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal32_PopReal32 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal32 << StackBehaviourPushOffset));

        public static readonly ILOpCode MUL_R64 = new ILOpCode(ILCode.MUL_R64,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.UNSIGNED) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal64_PopReal64 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal64 << StackBehaviourPushOffset));

        public static readonly ILOpCode DIV_DWORD = new ILOpCode(ILCode.DIV_DWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.UNSIGNED) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword_PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));

        public static readonly ILOpCode DIV_QWORD = new ILOpCode(ILCode.DIV_QWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.UNSIGNED) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword_PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushQword << StackBehaviourPushOffset));

        public static readonly ILOpCode DIV_R32 = new ILOpCode(ILCode.DIV_R32,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal32_PopReal32 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal32 << StackBehaviourPushOffset));

        public static readonly ILOpCode DIV_R64 = new ILOpCode(ILCode.DIV_R64,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal64_PopReal64 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal64 << StackBehaviourPushOffset));

        public static readonly ILOpCode REM_DWORD = new ILOpCode(ILCode.REM_DWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.UNSIGNED) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword_PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));

        public static readonly ILOpCode REM_QWORD = new ILOpCode(ILCode.REM_QWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.UNSIGNED) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword_PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushQword << StackBehaviourPushOffset));

        public static readonly ILOpCode REM_R32 = new ILOpCode(ILCode.REM_R32,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal32_PopReal32 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal32 << StackBehaviourPushOffset));

        public static readonly ILOpCode REM_R64 = new ILOpCode(ILCode.REM_R64,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal64_PopReal64 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal64 << StackBehaviourPushOffset));

        public static readonly ILOpCode SHR_DWORD = new ILOpCode(ILCode.SHR_DWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.UNSIGNED) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword_PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));

        public static readonly ILOpCode SHR_QWORD = new ILOpCode(ILCode.SHR_QWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.UNSIGNED) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword_PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushQword << StackBehaviourPushOffset));

        public static readonly ILOpCode SHL_DWORD = new ILOpCode(ILCode.SHL_DWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword_PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));

        public static readonly ILOpCode SHL_QWORD = new ILOpCode(ILCode.SHL_QWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword_PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushQword << StackBehaviourPushOffset));

        public static readonly ILOpCode FCONV_R32_R64 = new ILOpCode(ILCode.FCONV_R32_R64,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal32 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal64 << StackBehaviourPushOffset));

        public static readonly ILOpCode FCONV_R64_R32 = new ILOpCode(ILCode.FCONV_R64_R32,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopReal64 << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal32 << StackBehaviourPushOffset));

        public static readonly ILOpCode FCONV_R32 = new ILOpCode(ILCode.FCONV_R32,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal32 << StackBehaviourPushOffset));

        public static readonly ILOpCode FCONV_R64 = new ILOpCode(ILCode.FCONV_R64,
            ((byte) VMFlags.UNSIGNED << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal64 << StackBehaviourPushOffset));

        public static readonly ILOpCode ICONV_PTR = new ILOpCode(ILCode.ICONV_PTR,
            ((byte) VMFlags.OVERFLOW << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushPtr << StackBehaviourPushOffset));

        public static readonly ILOpCode ICONV_R64 = new ILOpCode(ILCode.ICONV_R64,
            ((byte) (VMFlags.OVERFLOW | VMFlags.UNSIGNED) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopQword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushReal64 << StackBehaviourPushOffset));

        public static readonly ILOpCode VCALL = new ILOpCode(ILCode.VCALL,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopVar << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushVar << StackBehaviourPushOffset));

        public static readonly ILOpCode TRY = new ILOpCode(ILCode.TRY,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopVar << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));

        public static readonly ILOpCode LEAVE = new ILOpCode(ILCode.LEAVE,
            ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.None << StackBehaviourPushOffset));
        
        // Pseudo opcodes.
        public static readonly ILOpCode __NOT_DWORD = new ILOpCode(ILCode.__NOT_DWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN) << AffectedFlagsOffset)
            |((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));

        public static ILOpCode __SUB_DWORD = new ILOpCode(ILCode.__SUB_DWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN | VMFlags.OVERFLOW | VMFlags.CARRY) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword_PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));
        
        public static readonly ILOpCode __OR_DWORD = new ILOpCode(ILCode.__OR_DWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword_PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));

        public static readonly ILOpCode __AND_DWORD = new ILOpCode(ILCode.__AND_DWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword_PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));

        public static readonly ILOpCode __XOR_DWORD = new ILOpCode(ILCode.__XOR_DWORD,
            ((byte) (VMFlags.ZERO | VMFlags.SIGN) << AffectedFlagsOffset)
            | ((byte) ILOperandType.None << OperandTypeOffset)
            | ((byte) ILFlowControl.Next << FlowControlOffset)
            | ((byte) ILStackBehaviour.PopDword_PopDword << StackBehaviourPopOffset)
            | ((byte) ILStackBehaviour.PushDword << StackBehaviourPushOffset));

    }
}