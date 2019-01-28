using System;
using Carp.Core.Disassembly;

namespace Carp.Core.Architecture
{
    public class ILInstruction
    {
        public ILInstruction(int offset, ILOpCode opCode, object operand)
        {
            Offset = offset;
            OpCode = opCode;
            Operand = operand;
        }
        
        public int Offset
        {
            get;
            set;
        }
        
        public ILOpCode OpCode
        {
            get;
            set;
        }

        public object Operand
        {
            get;
            set;
        }

        public ProgramState ProgramState
        {
            get;
            set;
        }

        public int Size
        {
            get
            {
                switch (OpCode.OperandType)
                {
                    case ILOperandType.None:
                        return 2;
                    case ILOperandType.Register:
                        return 3;
                    case ILOperandType.ImmediateDword:
                        return 6;
                    case ILOperandType.ImmediateQword:
                        return 10;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override string ToString()
        {
            return $"IL_{Offset:X4}: {OpCode} {Operand}";
        }
    }
}