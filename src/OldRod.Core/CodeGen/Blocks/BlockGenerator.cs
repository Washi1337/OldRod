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
using AsmResolver.PE.DotNet.Cil;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.DataFlow;
using Rivers;
using Rivers.Analysis;
using Rivers.Analysis.Connectivity;

namespace OldRod.Core.CodeGen.Blocks
{
    public class BlockGenerator
    {
        private readonly ControlFlowGraph _cfg;
        private readonly DominatorInfo _dominatorInfo;
        private readonly Graph _dominatorTree;
        private readonly Dictionary<Node, ISet<Node>> _nodeToComponent;
        private readonly CilCodeGenerator _generator;

        public BlockGenerator(ControlFlowGraph cfg, CilCodeGenerator generator)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            _generator = generator;

            _dominatorInfo = new DominatorInfo(cfg.Entrypoint);
            _dominatorTree = _dominatorInfo.ToDominatorTree();
            
            var components = cfg.Entrypoint.FindStronglyConnectedComponents();
            _nodeToComponent = new Dictionary<Node, ISet<Node>>();
            foreach (var component in components)
            {
                foreach (var node in component)
                    _nodeToComponent[node] = component;
            }
        }

        public IDictionary<Node, CilInstruction> BlockEntries
        {
            get;
        } = new Dictionary<Node, CilInstruction>();

        public IDictionary<Node, CilInstruction> BlockExits
        {
            get;
        } = new Dictionary<Node, CilInstruction>();

        public ScopeBlock CreateBlock()
        {
            var rootScope = new ScopeBlock();
            TraverseNode(rootScope, _cfg.Nodes, _cfg.Entrypoint);
            return rootScope;
        }
        
        private Node GetTreeNode(Node cfgNode)
        {
            return _dominatorTree.Nodes[cfgNode.Name];
        }

        private Node GetCfgNode(Node treeNode)
        {
            return _cfg.Nodes[treeNode.Name];
        }

        private IList<Node> TraverseNode(ScopeBlock parentScope, ICollection<Node> scope, Node node)
        {
            var unprocessedNodes = new List<Node>();
            
            var basicBlock = CreateBasicBlock(node);
            parentScope.Blocks.Add(basicBlock);

            var children = GetTreeNode(node).OutgoingEdges
                .Select(e => GetCfgNode(e.Target))
                .Where(n => n.IncomingEdges.All(IsNormalEdge))
                .ToArray();

            var directChildren = children
                .Where(c => c.GetPredecessors().Contains(node))
                .OrderByDescending(n => _nodeToComponent[n].Count)
                .ToArray();

            var indirectChildren = children
                .Except(directChildren)
                .OrderByDescending(n => _nodeToComponent[n].Count)
                .ToArray();
            
            foreach (var child in directChildren.Union(indirectChildren))
            {
                if (!scope.Contains(child))
                {
                    unprocessedNodes.Add(child);
                }
                else
                {
                    var ehScope = child.SubGraphs.Except(node.SubGraphs).ToArray();
                    switch (ehScope.Length)
                    {
                        case 0:
                            unprocessedNodes.AddRange(TraverseNode(parentScope, scope, child));
                            break;
                        case 1:
                            unprocessedNodes.AddRange(TraverseEHNode(parentScope, ehScope[0].Nodes, child, ehScope[0]));
                            break;
                        default:
                            // This would mean we enter multiple EHs at once, which is impossible in vanilla KoiVM.
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            TraverseUnprocessedNodes(parentScope, scope, unprocessedNodes);
            
            return unprocessedNodes;
        }

        private IList<Node> TraverseEHNode(ScopeBlock parentScope, ICollection<Node> scope, Node tryStart, SubGraph ehCluster)
        {
            var frame = (EHFrame) ehCluster.UserData[EHFrame.EHFrameProperty];
            var tryBody = (ICollection<Node>) ehCluster.UserData[ControlFlowGraph.TryBlockProperty];
            var handlerBody = (ICollection<Node>) ehCluster.UserData[ControlFlowGraph.HandlerBlockProperty];

            var handlerStart = _cfg.Nodes[_cfg.GetNodeName((long) frame.HandlerAddress)];
            
            var unprocessedNodes = new List<Node>();

            var ehBlock = new ExceptionHandlerBlock
            {
                TryBlock = new ScopeBlock(),
                HandlerBlock = new ScopeBlock()
            };
            parentScope.Blocks.Add(ehBlock);

            unprocessedNodes.AddRange(TraverseNode(ehBlock.TryBlock, tryBody, tryStart));
            unprocessedNodes.AddRange(TraverseNode(ehBlock.HandlerBlock, handlerBody, handlerStart));

            return unprocessedNodes;
        }

        private void TraverseUnprocessedNodes(ScopeBlock parentScope, ICollection<Node> scope, List<Node> unprocessedNodes)
        {
            foreach (var node in unprocessedNodes.ToArray().Where(scope.Contains))
            {
                unprocessedNodes.Remove(node);
                unprocessedNodes.AddRange(TraverseNode(parentScope, scope, node));
            }
        }

        private BasicBlock CreateBasicBlock(Node node)
        {
            var astBlock = (CilAstBlock) node.UserData[CilAstBlock.AstBlockProperty];
            var instructions = astBlock.AcceptVisitor(_generator);

            BlockEntries[node] = instructions[0];
            BlockExits[node] = instructions[instructions.Count - 1];
            
            return new BasicBlock(instructions);
        }

        private static bool IsNormalEdge(Edge edge)
        {
            if (!edge.UserData.TryGetValue(ControlFlowGraph.ConditionProperty, out var c))
                return true;

            var conditions = (ICollection<int>) c;

            return !conditions.Contains(ControlFlowGraph.ExceptionConditionLabel);
//                   && !conditions.Contains(ControlFlowGraph.EndFinallyConditionLabel);
        }
        
    }
}