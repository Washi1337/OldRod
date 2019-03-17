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

using System;
using System.Collections.Generic;
using System.Linq;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.DataFlow;
using OldRod.Core.Disassembly.Inference;
using Rivers;

namespace OldRod.Core.Disassembly.ControlFlow
{
    public class ControlFlowGraphBuilder
    {
        public static ControlFlowGraph BuildGraph(VMExportDisassembly disassembly)
        {
            var graph = new ControlFlowGraph();

            CollectBlocks(graph, disassembly.Instructions.Values, disassembly.BlockHeaders);
            ConnectNodes(graph);
            CreateEHClusters(graph);

            graph.Entrypoint = graph.Nodes[graph.GetNodeName(disassembly.ExportInfo.CodeOffset)];
            return graph;
        }

        private static void CollectBlocks(ControlFlowGraph graph, ICollection<ILInstruction> instructions, ICollection<long> blockHeaders)
        {
            Node currentNode = null;
            ILBasicBlock currentBlock = null;
            foreach (var instruction in instructions.OrderBy(x => x.Offset))
            {
                // If current instruction is a basic block header, we start a new block. 
                if (currentNode == null || blockHeaders.Contains(instruction.Offset))
                {
                    currentNode = graph.Nodes.Add(graph.GetNodeName(instruction.Offset));
                    currentBlock = new ILBasicBlock();
                    currentNode.UserData[ILBasicBlock.BasicBlockProperty] = currentBlock;
                }

                // Add instruction to current block.
                currentBlock.Instructions.Add(instruction);

                // If next offset is also a header, we also create a new block.
                // This check is necessary as blocks might not appear in sequence after each other. 
                if (blockHeaders.Contains(instruction.Offset + instruction.Size))
                    currentNode = null;
            }
        }

        private static void ConnectNodes(ControlFlowGraph graph)
        {
            foreach (var node in graph.Nodes)
            {
                // Get the last instruction of the block.
                var block = (ILBasicBlock) node.UserData[ILBasicBlock.BasicBlockProperty];
                var last = block.Instructions[block.Instructions.Count - 1];
                long nextOffset = last.Offset + last.Size;

                // Add edges accordingly.
                switch (last.OpCode.FlowControl)
                {
                    case ILFlowControl.Next:
                        AddFallThroughEdge(graph, node, nextOffset);
                        break;
                    case ILFlowControl.Jump:
                        AddJumpTargetEdges(graph, node, last);
                        break;
                    case ILFlowControl.ConditionalJump:
                        AddJumpTargetEdges(graph, node, last);
                        AddFallThroughEdge(graph, node, nextOffset);
                        break;
                    case ILFlowControl.Call:
                        break;
                    case ILFlowControl.Return:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void AddFallThroughEdge(ControlFlowGraph graph, Node node, long nextOffset)
        {
            node.OutgoingEdges.Add(graph.GetNodeName(nextOffset));
        }

        private static void AddJumpTargetEdges(ControlFlowGraph graph, Node node, ILInstruction jump)
        {
            var jumpMetadata = (JumpAnnotation) jump.Annotation;
            for (int i = 0; i < jumpMetadata.InferredJumpTargets.Count; i++)
            {
                ulong target = jumpMetadata.InferredJumpTargets[i];
                var edge = new Edge(node, graph.Nodes[graph.GetNodeName((long) target)]);
                edge.UserData[ControlFlowGraph.ConditionProperty] = i;
                graph.Edges.Add(edge);
            }
        }

        private static void CreateEHClusters(ControlFlowGraph graph)
        {
            var clusters = new Dictionary<EHFrame, SubGraph>();
            
            foreach (var node in graph.Nodes)
            {
                var block = (ILBasicBlock) node.UserData[ILBasicBlock.BasicBlockProperty];
                var state = block.Instructions[0].ProgramState;
                
                foreach (var frame in state.EHStack)
                {
                    if (!clusters.TryGetValue(frame, out var subGraph))
                    {
                        subGraph = new SubGraph(graph, frame.ToString());
                        subGraph.UserData[EHFrame.EHFrameProperty] = frame;
                        graph.SubGraphs.Add(subGraph);
                        clusters.Add(frame, subGraph);
                    }
                    
                    subGraph.Nodes.Add(node);    
                }
            }
        }
        
    }
}