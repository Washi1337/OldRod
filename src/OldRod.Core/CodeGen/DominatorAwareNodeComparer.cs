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
        private DominatorInfo _dominatorInfo;
        private Graph _dominatorTree;

        public DominatorAwareNodeComparer(ControlFlowGraph cfg, DominatorInfo dominatorInfo, Graph dominatorTree)
        {
            _cfg = cfg;
            _dominatorInfo = dominatorInfo;
            _dominatorTree = dominatorTree;
        }
        
        public Node CurrentNode
        {
            get;
            set;
        }
        
        public int Compare(Node x, Node y)
        {
            if (x == null || y == null)
                throw new ArgumentNullException();
            if (x == y)
                return 0;

            bool xIsDirectChild = CurrentNode.GetSuccessors().Any(s => s.Name == x.Name);
            bool yIsDirectChild = CurrentNode.GetSuccessors().Any(s => s.Name == y.Name);

            if (xIsDirectChild && !yIsDirectChild)
                return -1;
            if (!xIsDirectChild && yIsDirectChild)
                return 1;
            
            bool xIsHandlerStart = x.UserData.TryGetValue(ControlFlowGraph.HandlerStartProperty, out var xFrame);
            bool yIsHandlerStart = y.UserData.TryGetValue(ControlFlowGraph.HandlerStartProperty, out var yFrame);

            if (xIsHandlerStart && yIsHandlerStart)
                return _dominatorInfo.Dominates(x, y) ? -1 : 1;

            if (xIsHandlerStart)
            {
                var ehFrame = (EHFrame) xFrame;
                var ehCluster = x.SubGraphs.First(s => s.Name == _cfg.GetClusterName(ehFrame));

                return ehCluster.Nodes.Contains(y) ? 1 : -1;
            }

            if (yIsHandlerStart)
            {
                var ehFrame = (EHFrame) yFrame;
                var ehCluster = y.SubGraphs.First(s => s.Name == _cfg.GetClusterName(ehFrame));

                return ehCluster.Nodes.Contains(x) ? -1 : 1;
            }

            return _dominatorInfo.Dominates(_dominatorInfo.GetImmediateDominator(x), y) ? -1 : 1;
        }
        
    }
}