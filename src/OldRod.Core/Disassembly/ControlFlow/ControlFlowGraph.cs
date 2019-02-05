using Rivers;

namespace OldRod.Core.Disassembly.ControlFlow
{
    public class ControlFlowGraph : Graph
    {
        public const string ConditionProperty = "label";
        
        public Node Entrypoint
        {
            get;
            set;
        }

        public Node FindBlockWithOffset(long offset)
        {
            foreach (var node in Nodes)
            {
                if (node.UserData[ILBasicBlock.BasicBlockProperty] is ILBasicBlock block
                    && block.Instructions[0].Offset >= offset
                    && block.Instructions[block.Instructions.Count - 1].Offset < offset)
                {
                    return node;
                }
            }

            return null;
        }

        public string GetNodeName(long startOffset)
        {
            return "Block_" + startOffset.ToString("X4");
        } 
    }
}