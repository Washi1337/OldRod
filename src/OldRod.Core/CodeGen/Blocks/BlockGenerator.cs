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
        private readonly CilCodeGenerator _generator;

        public BlockGenerator(ControlFlowGraph cfg, CilCodeGenerator generator)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            _generator = generator;
            
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
            var sorter = new TopologicalSorter(n =>
            {
                return n.OutgoingEdges
                    .OrderByDescending(IsNormalEdge)
                    .Select(e => e.Target)
                    .ToList();
            });

            var sorting = sorter.GetTopologicalSorting(_cfg.Entrypoint);
            
            var rootScope = new ScopeBlock();
            foreach (var node in sorting.Reverse())
                rootScope.Blocks.Add(CreateBasicBlock(node));
            
            return rootScope;
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

            return !conditions.Contains(ControlFlowGraph.ExceptionConditionLabel)
                   && !conditions.Contains(ControlFlowGraph.EndFinallyConditionLabel);
        }
        
    }
}