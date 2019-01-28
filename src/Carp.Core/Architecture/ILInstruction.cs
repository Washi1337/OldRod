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

        public override string ToString()
        {
            return $"IL_{Offset:X4}: {OpCode} {Operand}";
        }
    }
}