namespace OldRod.Core.Architecture
{
    public struct ILOpCode
    {
        internal ILOpCode(ILCode code, int flags)
        {
            Code = code;
            OperandType = (ILOperandType) (flags & 0xFF);
            FlowControl = (ILFlowControl) ((flags >> 8) & 0xFF);
            StackBehaviourPop = (ILStackBehaviour) ((flags >> 16) & 0xFF);
            StackBehaviourPush = (ILStackBehaviour) ((flags >> 24) & 0xFF);
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

        public ILStackBehaviour StackBehaviourPush
        {
            get;
        }

        public ILStackBehaviour StackBehaviourPop
        {
            get;
        }

        public override string ToString()
        {
            return Code.ToString();
        }
    }
}