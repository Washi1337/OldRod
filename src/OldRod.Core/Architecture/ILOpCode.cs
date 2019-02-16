namespace OldRod.Core.Architecture
{
    public struct ILOpCode
    {
        private readonly int _flags;

        internal ILOpCode(ILCode code, int flags)
        {
            _flags = flags;
            Code = code;
            ILOpCodes.All[(int) code] = this;
        }

        public ILCode Code
        {
            get;
        }

        public ILOperandType OperandType => (ILOperandType) (_flags & 0xFF);

        public ILFlowControl FlowControl => (ILFlowControl) ((_flags >> 8) & 0xFF);

        public ILStackBehaviour StackBehaviourPop => (ILStackBehaviour) ((_flags >> 16) & 0xFF);
        
        public ILStackBehaviour StackBehaviourPush => (ILStackBehaviour) ((_flags >> 24) & 0xFF);

        public override string ToString()
        {
            return Code.ToString();
        }
    }
}