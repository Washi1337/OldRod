using System.Collections.Generic;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.ControlFlow
{
    public class ILBasicBlock
    {
        public const string BasicBlockProperty = "basicblock";
        
        public IList<ILInstruction> Instructions
        {
            get;
        } = new List<ILInstruction>();
    }
}