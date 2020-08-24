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

using System.Collections.Generic;
using System.Linq;
using OldRod.Core.Disassembly.ControlFlow;

namespace OldRod.Core.Ast.Cil
{
    public class CilCompilationUnit : CilAstNode
    {
        public CilCompilationUnit(ControlFlowGraph graph)
        {
            ControlFlowGraph = graph;
        }
        
        public ICollection<CilVariable> Variables
        {
            get;
        } = new List<CilVariable>();
        
        public ICollection<CilParameter> Parameters
        {
            get;
        } = new List<CilParameter>();

        public CilVariable FlagVariable
        {
            get;
            set;
        }
        
        public ControlFlowGraph ControlFlowGraph
        {
            get;
        }
        
        public override void ReplaceNode(CilAstNode node, CilAstNode newNode)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerable<CilAstNode> GetChildren()
        {
            return ControlFlowGraph.Nodes.Select(x => (CilAstBlock) x.UserData[CilAstBlock.AstBlockProperty]);
        }

        public override void AcceptVisitor(ICilAstVisitor visitor)
        {
            visitor.VisitCompilationUnit(this);
        }

        public override TResult AcceptVisitor<TResult>(ICilAstVisitor<TResult> visitor)
        {
            return visitor.VisitCompilationUnit(this);
        }
        
    }
}