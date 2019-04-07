using System;
using System.Collections.Generic;
using System.Linq;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.DataFlow;
using Rivers;
using Rivers.Analysis;

namespace OldRod.Core.CodeGen
{
    public class DominatorAwareNodeComparer : IComparer<Node>
    {
        private readonly ControlFlowGraph _cfg;
        private readonly DominatorInfo _dominatorInfo;
        private readonly Graph _dominatorTree;
        
        private readonly ISet<Node> _directChildren = new HashSet<Node>();
        private readonly ISet<Node> _indirectChildren = new HashSet<Node>();
        
        private Node _currentNode;
        private EHFrame _ehFrame;
        private ICollection<Node> _tryBody;
        private Node _handlerEntry;

        public DominatorAwareNodeComparer(ControlFlowGraph cfg, DominatorInfo dominatorInfo, Graph dominatorTree)
        {
            _cfg = cfg;
            _dominatorInfo = dominatorInfo;
            _dominatorTree = dominatorTree;
        }

        public Node CurrentNode
        {
            get => _currentNode;
            set
            {
                _currentNode = value;
                
                // Check whether the current node is a try entry block.
                _currentNode.UserData.TryGetValue(ControlFlowGraph.TryStartProperty, out var frame);
                _ehFrame = frame as EHFrame;

                if (_ehFrame != null)
                {
                    // If it is, find the nodes that make part of just the try block, since we need to prioritize those
                    // nodes before we do the handler block and the rest of the nodes in the CFG.
                    _handlerEntry = _cfg.Nodes[_cfg.GetNodeName((long) _ehFrame.HandlerAddress)];
                    var ehCluster = value.SubGraphs.First(x => x.Name == _cfg.GetClusterName(_ehFrame));
                    _tryBody = (ICollection<Node>) ehCluster.UserData[ControlFlowGraph.TryBlockProperty];
                }

                // Collect direct and indirect children in the dominator tree.
                _directChildren.Clear();
                _indirectChildren.Clear();
                foreach (var treeNodeChild in _dominatorTree.Nodes[value.Name].GetSuccessors())
                {
                    var cfgChild = _cfg.Nodes[treeNodeChild.Name];
                    bool isDirectChild = value.GetSuccessors().Any(n => n.Name == treeNodeChild.Name);
                    if (isDirectChild)
                        _directChildren.Add(cfgChild);
                    else
                        _indirectChildren.Add(cfgChild);
                }
            }
        }

        private bool IsTryStart => _ehFrame != null;

        public int Compare(Node x, Node y)
        {
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                throw new ArgumentNullException();
            if (x == y)
                return 0;

            if (IsTryStart)
            {
                if (x == _handlerEntry)
                {
                    // Prioritize try block nodes.
                    if (_tryBody.Contains(y))
                        return 1;
                    
                    // Handler blocks before remaining nodes outside the EH.
                    return -1;
                }

                if (y == _handlerEntry)
                {
                    // Prioritize try block nodes.
                    if (_tryBody.Contains(x))
                        return -1;
                    
                    // Handler blocks before remaining nodes outside the EH.
                    return 1;
                }

                // Prioritize try block nodes.
                if (_tryBody.Contains(x) && !_tryBody.Contains(y))
                    return -1;
                if (!_tryBody.Contains(x) && _tryBody.Contains(y))
                    return 1;
            }
            
            // Two direct children can appear in any order, so they are considered equal.
            if (_directChildren.Contains(x) && _directChildren.Contains(y))
                return 0;

            // Prioritize direct children.
            return _directChildren.Contains(x) 
                ? -1 // x is a direct child. 
                : 1; // y is a direct child.
        }
        
    }
}