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

namespace OldRod.Core.Architecture
{
    public struct ILOpCode
    {
        internal const int AffectedFlagsOffset       = 0;
        internal const int AffectedFlagsMask         = 0b11111111;
        internal const int OperandTypeOffset         = 8;
        internal const int OperandTypeMask           = 0b11;
        internal const int FlowControlOffset         = 10;
        internal const int FlowControlMask           = 0b111;
        internal const int StackBehaviourPopOffset   = 13;
        internal const int StackBehaviourPopMask     = 0b11111;
        internal const int StackBehaviourPushOffset  = 18;
        internal const int StackBehaviourPushMask    = 0b11111;
            
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

        public bool AffectsFlags => AffectedFlags != 0;

        public VMFlags AffectedFlags => (VMFlags) ((_flags >> AffectedFlagsOffset) & AffectedFlagsMask);

        public ILOperandType OperandType => (ILOperandType) ((_flags >> OperandTypeOffset) & OperandTypeMask);

        public ILFlowControl FlowControl => (ILFlowControl) ((_flags >> FlowControlOffset) & FlowControlMask);

        public ILStackBehaviour StackBehaviourPop => (ILStackBehaviour) ((_flags >> StackBehaviourPopOffset) & StackBehaviourPopMask);

        public ILStackBehaviour StackBehaviourPush => (ILStackBehaviour) ((_flags >> StackBehaviourPushOffset) & StackBehaviourPushMask);


        public override string ToString()
        {
            return Code.ToString();
        }
    }
}