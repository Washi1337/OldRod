using System;
using System.Collections.Generic;
using System.Linq;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Disassembly.ControlFlow;
using Rivers;
using Rivers.Analysis;
using Rivers.Analysis.Connectivity;

namespace OldRod.Core.CodeGen
{
    public class CfgNodeSorter
    {
        private readonly Graph _dominatorTree;
        private readonly DominatorInfo _dominatorInfo;
        private readonly IDictionary<Node, ISet<Node>> _nodeToComponent;

        public CfgNodeSorter(ControlFlowGraph controlFlowGraph)
        {
            ControlFlowGraph = controlFlowGraph ?? throw new ArgumentNullException(nameof(controlFlowGraph));
            _dominatorInfo = new DominatorInfo(controlFlowGraph.Entrypoint);
            _dominatorTree = _dominatorInfo.ToDominatorTree();
            
            var components = controlFlowGraph.FindStronglyConnectedComponents();
            _nodeToComponent = new Dictionary<Node, ISet<Node>>();
            foreach (var component in components)
            {
                foreach (var node in component)
                    _nodeToComponent[node] = component;
            }
        }

        public ControlFlowGraph ControlFlowGraph
        {
            get;
        }

        public IList<Node> GetSortedNodes()
        {
            // Start collecting nodes at the entrypoint with the entire method body as the scope.
            return CollectSortedNodes(ControlFlowGraph.Nodes, ControlFlowGraph.Entrypoint);
        }

        private IList<Node> CollectSortedNodes(ICollection<Node> scope, Node entry)
        {
            var result = new List<Node> {entry};

            var (normal, abnormal, indirect) = GetChildren(scope, entry);

            // Prioritize normal edges over abnormal and remaining edges.
            AddNodes(result, scope, entry, normal);
            AddNodes(result, scope, entry, abnormal);
            AddNodes(result, scope, entry, indirect);
            
            return result;
        }

        private void AddNodes(List<Node> result, ICollection<Node> scope, Node entry, IEnumerable<Node> nodes)
        {
            // Order children by size of the connected component they are part of.
            foreach (var child in nodes
                .OrderByDescending(n => _nodeToComponent[n].Count))
            {
                // Check whether the node enters a new scope, and if so, use it.
                var innerScope = child.SubGraphs.Except(entry.SubGraphs).FirstOrDefault();
                result.AddRange(CollectSortedNodes(innerScope?.Nodes ?? scope, child));
            }
        }

        private Node GetTreeNode(Node cfgNode)
        {
            return _dominatorTree.Nodes[cfgNode.Name];
        }

        private Node GetCfgNode(Node treeNode)
        {
            return ControlFlowGraph.Nodes[treeNode.Name];
        }

        private (IEnumerable<Node> normal, IEnumerable<Node> abnormal, IEnumerable<Node> indirect) GetChildren(
            ICollection<Node> scope, Node node)
        {
            var treeNode = GetTreeNode(node);

            var directEdges = treeNode.OutgoingEdges
                .Where(e => node.OutgoingEdges.Contains(e.Target.Name) && scope.Contains(GetCfgNode(e.Target)))
                .ToArray();

            var indirectEdges = treeNode.OutgoingEdges.Except(directEdges);

            return (
                directEdges
                    .Where(IsNormalEdge)
                    .Select(e => GetCfgNode(e.Target)),
                directEdges
                    .Where(e => !IsNormalEdge(e))
                    .Select(e => GetCfgNode(e.Target)),
                indirectEdges
                    .Select(e => GetCfgNode(e.Target))
            );
        }
        private bool IsNormalEdge(Edge edge)
        {
            edge = ControlFlowGraph.Edges[edge.Source.Name, edge.Target.Name];
            if (!edge.UserData.TryGetValue(ControlFlowGraph.ConditionProperty, out var c))
                return true;

            var condition = (ICollection<int>) c;
            return !condition.Contains(ControlFlowGraph.ExceptionConditionLabel)
                   && !condition.Contains(ControlFlowGraph.EndFinallyConditionLabel);
        }

    }
}