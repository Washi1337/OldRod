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
using Rivers.Analysis;
using Rivers.Serialization.Dot;

namespace OldRod.Core.Disassembly.ControlFlow
{
    public class ControlFlowGraphBuilder
    {
        public const int ExceptionConditionLabel = -1;
        public const int EndFinallyConditionLabel = -2;
        
        public static ControlFlowGraph BuildGraph(VMFunction function)
        {
            var graph = new ControlFlowGraph();

            CollectBlocks(graph, function.Instructions.Values, function.BlockHeaders);
            ConnectNodes(graph);
            CreateEHClusters(graph);
            AddAbnormalEdges(graph);

            graph.Entrypoint = graph.Nodes[graph.GetNodeName(function.EntrypointAddress)];
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
                        subGraph = new SubGraph(graph, graph.GetClusterName(frame));
                        subGraph.UserData[EHFrame.EHFrameProperty] = frame;
                        graph.SubGraphs.Add(subGraph);
                        clusters.Add(frame, subGraph);
                    }
                    
                    subGraph.Nodes.Add(node);
                }
            }
        }

        private static void AddAbnormalEdges(ControlFlowGraph graph)
        {
            if (graph.SubGraphs.Count == 0)
                return;

            new DotWriter(Console.Out).Write(graph);
            
            var handlerExits = new Dictionary<EHFrame, ICollection<Node>>();
            
            // Since exception handlers make it possible to transfer control to the handler block
            // at any time, we have these "abnormal edges" from each node in the try block 
            // to the first node in the handler block.

            // Go over each EH in ascending order, so smallest (and therefore nested) EHs are processed first. 
            foreach (var subGraph in graph.SubGraphs.OrderBy(x => x.Nodes.Count))
            {
                var ehFrame = (EHFrame) subGraph.UserData[EHFrame.EHFrameProperty];

                // Find the try entry node.
                var tryEntry = graph.Nodes[graph.GetNodeName((long) ehFrame.TryStart)];
                tryEntry.UserData[ControlFlowGraph.TryStartProperty] = ehFrame;

                // Find the handler entry node.
                var handlerEntry = graph.Nodes[graph.GetNodeName((long) ehFrame.HandlerAddress)];
                handlerEntry.UserData[ControlFlowGraph.HandlerStartProperty] = ehFrame;

                // Determine the handler exits.
                // TODO: add scope in Rivers for better efficiency.
                var dominatorInfo = new DominatorInfo(handlerEntry); 
                var handlerBody = dominatorInfo.GetDominatedNodes(handlerEntry);
                handlerExits.Add(ehFrame, new HashSet<Node>(handlerBody.Where(x => x.OutgoingEdges.Count == 0)));

                // Add for each node in the try block an abnormal edge.
                var tryBody = new HashSet<Node>(subGraph.Nodes.Except(handlerBody));

                foreach (var node in tryBody.Where(n => !n.UserData.ContainsKey(ControlFlowGraph.TopMostEHProperty)))
                {
                    node.UserData[ControlFlowGraph.TopMostEHProperty] = ehFrame;
                    var edge = new Edge(node, handlerEntry);
                    edge.UserData[ControlFlowGraph.ConditionProperty] = ExceptionConditionLabel;
                    graph.Edges.Add(edge);
                }

                subGraph.UserData[ControlFlowGraph.TryBlockProperty] = tryBody;
                subGraph.UserData[ControlFlowGraph.HandlerBlockProperty] = handlerBody;
            }

            // Since a LEAVE instruction might not directly transfer control to the referenced instruction,
            // but rather transfer control to a finally block first,  we have to add edges to these nodes 
            // as well.
            
            foreach (var node in graph.Nodes)
            {
                if (node.SubGraphs.Count > 0)
                {
                    // Check if the node ends with a LEAVE.
                    var block = (ILBasicBlock) node.UserData[ILBasicBlock.BasicBlockProperty];
                    var last = block.Instructions[block.Instructions.Count - 1];
                    if (last.OpCode.Code == ILCode.LEAVE)
                    {
                        // Find the frame we're jumping out of.
                        var ehFrame = last.ProgramState.EHStack.Peek();

                        // Add for each handler exit an edge to the referenced instruction.
                        foreach (var exit in handlerExits[ehFrame])
                        {
                            var edge = new Edge(exit, node.OutgoingEdges.First().Target);
                            edge.UserData[ControlFlowGraph.ConditionProperty] = EndFinallyConditionLabel;
                            graph.Edges.Add(edge);
                        }
                    }
                }
            }
            
        }
        
    }
}