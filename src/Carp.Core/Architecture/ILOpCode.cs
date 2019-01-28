namespace Carp.Core.Architecture
{
    public struct ILOpCode
    {
        internal ILOpCode(ILCode code, int flags)
        {
            Code = code;
            OperandType = (ILOperandType) (flags & 0xFF);
            FlowControl = (ILFlowControl) ((flags >> 8) & 0xFF);
            ILOpCodes.All[(int) code] = this;
        }

        public ILCode Code
        {
            get;
        }

        public ILOperandType OperandType
        {
            get;
        }

        public ILFlowControl FlowControl
        {
            get;
        }

        public override string ToString()
        {
            return Code.ToString();
        }
    }
}