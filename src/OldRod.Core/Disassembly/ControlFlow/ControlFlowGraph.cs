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

using OldRod.Core.Disassembly.DataFlow;
using Rivers;

namespace OldRod.Core.Disassembly.ControlFlow
{
    public class ControlFlowGraph : Graph
    {
        public const string TopMostEHProperty = "topmosteh";
        public const string ConditionProperty = "label";
        public const string TryBlockProperty = "try";
        public const string HandlerBlockProperty = "handler";
        public const string TryStartProperty = "trystart";
        public const string HandlerStartProperty = "handlerstart";

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

        public string GetClusterName(EHFrame frame)
        {
            return frame.ToString();
        }
    }
}