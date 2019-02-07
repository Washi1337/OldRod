using System;
using System.Collections.Generic;
using System.Linq;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Inference;
using Rivers;

namespace OldRod.Core.Disassembly.ControlFlow
{
    public class ControlFlowGraphBuilder
    {
        public static ControlFlowGraph BuildGraph(VMExportInfo export, ICollection<ILInstruction> instructions, ICollection<long> blockHeaders)
        {
            var graph = new ControlFlowGraph();

            CollectBlocks(graph, instructions, blockHeaders);
            ConnectNodes(graph);

            graph.Entrypoint = graph.Nodes[graph.GetNodeName(export.CodeOffset)];
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
            var jumpMetadata = (JumpMetadata) jump.InferredMetadata;
            for (var i = 0; i < jumpMetadata.InferredJumpTargets.Count; i++)
            {
                var target = jumpMetadata.InferredJumpTargets[i];
                var edge = new Edge(node, graph.Nodes[graph.GetNodeName((long) target)]);
                edge.UserData[ControlFlowGraph.ConditionProperty] = i;
                graph.Edges.Add(edge);
            }
        }
    }
}