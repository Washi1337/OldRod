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
using System.Diagnostics;
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
        public const string Tag = "CFGBuilder";
        public ILogger Logger
        {
            get;
            set;
        } = EmptyLogger.Instance;

        public bool SalvageOnError
        {
            get;
            set;
        }
        
        public ControlFlowGraph BuildGraph(VMFunction function)
        {
            var graph = new ControlFlowGraph();

            CollectBlocks(graph, function.Instructions.Values, function.BlockHeaders);
            graph.Entrypoint = GetNode(graph, function.EntrypointAddress);
            ConnectNodes(graph);

            try
            {
                CreateEHClusters(graph);
                AddAbnormalEdges(graph);
            }
            catch (Exception ex) when (SalvageOnError)
            {
                Logger.Error(Tag,
                    $"Failed to create EH clusters in CFG of function_{function.EntrypointAddress:X4}. {ex.Message}");
            }

            return graph;
        }

        private Node GetNode(ControlFlowGraph graph, long offset)
        {
            string name = graph.GetNodeName(offset);
            
            if (!graph.Nodes.TryGetNode(name, out var node))
            {
                Logger.Error(Tag, $"Reference to an unexplored basic block IL_{offset:X4} found. Inserting dummy node.");
                node = graph.Nodes.Add(name);
            }

            return node;
        }

        private Edge CreateEdge(Node source, Node target, params int[] conditions)
        {
            ISet<int> edgeLabel = null;
            if (source.OutgoingEdges.TryGetEdge(target, out var edge))
            {
                edgeLabel = (ISet<int>) edge.UserData[ControlFlowGraph.ConditionProperty];
            }
            else
            {
                edge = new Edge(source, target);
                edgeLabel = new HashSet<int>();
                edge.UserData[ControlFlowGraph.ConditionProperty] = edgeLabel;
            }

            edgeLabel.UnionWith(conditions);
            return edge;
        }

        private T GetUserData<T>(Node node, string property)
        {
            if (!node.UserData.TryGetValue(property, out var value))
                return default(T);
            return (T) value;
        }

        private void CollectBlocks(ControlFlowGraph graph, ICollection<ILInstruction> instructions, ICollection<long> blockHeaders)
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

        private void ConnectNodes(ControlFlowGraph graph)
        {
            foreach (var node in graph.Nodes.ToArray())
            {
                var block = GetUserData<ILBasicBlock>(node, ILBasicBlock.BasicBlockProperty);
                if (block != null)
                {
                    var last = block.Instructions[block.Instructions.Count - 1];
                    AddNormalEdges(graph, last.OpCode.FlowControl, node);
                }
            }
        }

        private void AddNormalEdges(ControlFlowGraph graph, ILFlowControl flowControl, Node node)
        {
            // Get the last instruction of the block.
            var block = GetUserData<ILBasicBlock>(node, ILBasicBlock.BasicBlockProperty);
            var last = block.Instructions[block.Instructions.Count - 1];
            long nextOffset = last.Offset + last.Size;
            switch (flowControl)
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
                case ILFlowControl.Return:
                    break;
                case ILFlowControl.VCall:
                    var annotation = (VCallAnnotation) last.Annotation;
                    AddNormalEdges(graph, annotation?.VMCall.GetImpliedFlowControl() ?? ILFlowControl.Return, node);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AddFallThroughEdge(ControlFlowGraph graph, Node node, long nextOffset)
        {
            node.OutgoingEdges.Add(GetNode(graph, nextOffset));
        }

        private void AddJumpTargetEdges(ControlFlowGraph graph, Node node, ILInstruction jump)
        {
            if (jump.Annotation is not JumpAnnotation jumpMetadata)
                return;

            for (int i = 0; i < jumpMetadata.InferredJumpTargets.Count; i++)
            {
                ulong target = jumpMetadata.InferredJumpTargets[i];
                var edge = CreateEdge(node, GetNode(graph, (long) target), i);
                graph.Edges.Add(edge);
            }
        }

        private void CreateEHClusters(ControlFlowGraph graph)
        {
            var clusters = new Dictionary<EHFrame, SubGraph>();
            
            foreach (var node in graph.Nodes.ToArray())
            {
                var block = GetUserData<ILBasicBlock>(node, ILBasicBlock.BasicBlockProperty);
                if (block == null)
                    continue;
                
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

        private void AddAbnormalEdges(ControlFlowGraph graph)
        {
            if (graph.SubGraphs.Count == 0)
                return;
            
            // Since exception handlers make it possible to transfer control to the handler block
            // at any time, we have these "abnormal edges" from each node in the try block 
            // to the first node in the handler block (or filter block).

            // First, add the initial abnormal handler edges from every try-start to the handler-start (or filter start).
            // This allows us to do some more dominator-magic, where we can just infer the handler body based
            // on the nodes dominated by the handler node.
            foreach (var subGraph in graph.SubGraphs.OrderBy(x => x.Nodes.Count))
            {
                var ehFrame = (EHFrame) subGraph.UserData[EHFrame.EHFrameProperty];

                // Find the try entry node.
                var tryEntry = GetNode(graph, (long) ehFrame.TryStart);
                tryEntry.UserData[ControlFlowGraph.TryStartProperty] = ehFrame;

                // Find the handler entry node.
                var handlerEntry = GetNode(graph, (long) ehFrame.HandlerAddress);
                handlerEntry.UserData[ControlFlowGraph.HandlerStartProperty] = ehFrame;

                // Add initial abnormal edge.
                if (ehFrame.Type != EHType.FILTER)
                {
                    // Jump straight to the handler entry.
                    AddAbnormalEdge(graph, tryEntry, ehFrame, handlerEntry);
                }
                else
                {
                    // Jump to the filter entry.
                    var filterEntry = GetNode(graph, (long) ehFrame.FilterAddress);
                    filterEntry.UserData[ControlFlowGraph.FilterStartProperty] = ehFrame;
                    AddAbnormalEdge(graph, tryEntry, ehFrame, filterEntry);
                    
                    // Connect all terminators of the filter block to the handler entry.
                    foreach (var terminator in FindReachableReturnNodes(filterEntry))
                        AddAbnormalEdge(graph, terminator, ehFrame, handlerEntry);
                }
            }

            // Obtain dominator info.
            var dominatorInfo = new DominatorInfo(graph.Entrypoint);            
            
            // Add all handler nodes to the cluster, and add abnormal edges for each try node to the handler start node.
            var handlerExits = new Dictionary<EHFrame, ICollection<Node>>();
            foreach (var subGraph in graph.SubGraphs.OrderBy(x => x.Nodes.Count))
            {
                var ehFrame = (EHFrame) subGraph.UserData[EHFrame.EHFrameProperty];
                var tryEntry = GetNode(graph, (long) ehFrame.TryStart);
                var handlerEntry = GetNode(graph, (long) ehFrame.HandlerAddress);

                // Determine the handler exits.
                var handlerBody = CollectHandlerNodes(handlerEntry, dominatorInfo.GetDominatedNodes(handlerEntry));
                foreach (var handlerNode in handlerBody)
                    subGraph.Nodes.Add(handlerNode);
                
                handlerExits.Add(ehFrame, new HashSet<Node>(handlerBody.Where(x => x.OutgoingEdges.Count == 0)));

                // Add for each node in the try block an abnormal edge.
                var tryBody = new HashSet<Node>(subGraph.Nodes.Except(handlerBody));
                foreach (var node in tryBody.Where(n =>
                    !n.UserData.ContainsKey(ControlFlowGraph.TopMostEHProperty) && n != tryEntry))
                {
                    AddAbnormalEdge(graph, node, ehFrame, handlerEntry);
                }

                // Register EH boundaries in the sub graph.
                subGraph.UserData[ControlFlowGraph.TryBlockProperty] = tryBody;
                subGraph.UserData[ControlFlowGraph.HandlerBlockProperty] = handlerBody;
                
                if (ehFrame.Type == EHType.FILTER)
                {
                    var filterEntry = GetNode(graph, (long) ehFrame.FilterAddress);
                    subGraph.UserData[ControlFlowGraph.FilterStartProperty] = filterEntry;
                }
            }

            // Since a LEAVE instruction might not directly transfer control to the referenced instruction,
            // but rather transfer control to a finally block first,  we have to add edges to these nodes 
            // as well.
            
            foreach (var node in graph.Nodes)
            {
                if (node.SubGraphs.Count > 0)
                {
                    // Check if the node ends with a LEAVE.
                    var block = GetUserData<ILBasicBlock>(node, ILBasicBlock.BasicBlockProperty);
                    if (block == null)
                        continue;
                    
                    var last = block.Instructions[block.Instructions.Count - 1];
                    if (last.OpCode.Code == ILCode.LEAVE)
                    {
                        // Find the frame we're jumping out of.
                        var ehFrame = last.ProgramState.EHStack.Peek();

                        // Add for each handler exit an edge to the referenced instruction.
                        foreach (var exit in handlerExits[ehFrame])
                        {
                            var edge = CreateEdge(exit, node.OutgoingEdges.First().Target, ControlFlowGraph.EndFinallyConditionLabel);
                            exit.UserData[ControlFlowGraph.TopMostEHProperty] = ehFrame;
                            graph.Edges.Add(edge);
                        }
                    }
                }
            }
            
        }

        private void AddAbnormalEdge(ControlFlowGraph graph, Node node, EHFrame ehFrame, Node handlerEntry)
        {
            node.UserData[ControlFlowGraph.TopMostEHProperty] = ehFrame;
            var edge = CreateEdge(node, handlerEntry, 
                ControlFlowGraph.ExceptionConditionLabel);
            graph.Edges.Add(edge);
        }

        private IEnumerable<Node> FindReachableReturnNodes(Node filterEntry)
        {
            var agenda = new Stack<Node>();
            var visited = new HashSet<Node>();
            agenda.Push(filterEntry);

            while (agenda.Count > 0)
            {
                var current = agenda.Pop();
                if (!visited.Add(current))
                    continue;
                
                var block = GetUserData<ILBasicBlock>(current, ILBasicBlock.BasicBlockProperty);
                if (block == null)
                    continue;
                
                var last = block.Instructions[block.Instructions.Count - 1];
                if (last.OpCode.Code == ILCode.RET)
                {
                    yield return current;
                    continue;
                }

                foreach (var edge in current.OutgoingEdges)
                {
                    if (!edge.UserData.TryGetValue(ControlFlowGraph.ConditionProperty, out object data))
                    {
                        agenda.Push(edge.Target);
                        continue;
                    }

                    // Exclude abnormal edges.
                    var conditions = (ISet<int>) data; 
                    if (!conditions.Contains(ControlFlowGraph.ExceptionConditionLabel)
                        && !conditions.Contains(ControlFlowGraph.EndFinallyConditionLabel))
                    {
                        agenda.Push(edge.Target);
                    }
                }
            }
        }

        private ISet<Node> CollectHandlerNodes(Node handlerEntry, ISet<Node> scope)
        {
            int ehFrameCount = GetUserData<ILBasicBlock>(handlerEntry, ILBasicBlock.BasicBlockProperty).Instructions[0]
                .ProgramState.EHStack.Count;
            
            var result = new HashSet<Node>();
            
            var agenda = new Stack<Node>();
            var visited = new HashSet<Node>();
            agenda.Push(handlerEntry);

            while (agenda.Count > 0)
            {
                var current = agenda.Pop();
                if (!visited.Add(current) || !scope.Contains(current))
                    continue;

                var block = GetUserData<ILBasicBlock>(current, ILBasicBlock.BasicBlockProperty);
                if (block == null)
                    continue;

                var last = block.Instructions[block.Instructions.Count - 1];
                
                // HACK: The following is not exactly correct, this doesn't work for nested EHs inside handler blocks. 
                // We probably need a more sophisticated approach, one that does not rely on the original EH frame stack,
                // but our own EH frame stack that also pushes on entering handlers, and pops on leave / ret instructions.
                // As this requires quite some arch changes probably, this will require some thought. For now at least
                // this allows "recompilation" of the code, just incorrect nestings sometimes.
                if (last.ProgramState.EHStack.Count < ehFrameCount)
                    continue;

                result.Add(current);
                
                if (last.OpCode.Code is ILCode.RET or ILCode.LEAVE)
                    continue;
                
                foreach (var edge in current.OutgoingEdges)
                    agenda.Push(edge.Target);
            }

            return result;
        }
    }
}