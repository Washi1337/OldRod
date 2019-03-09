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
using OldRod.Core.Disassembly.ControlFlow;
using Rivers;
using Rivers.Analysis;

namespace OldRod.Core.Ast.IL
{
    public class ILCompilationUnit : ILAstNode
    {
        private readonly IDictionary<string, ILVariable> _variables = new Dictionary<string, ILVariable>();

        public ILCompilationUnit(VMFunctionSignature signature, ControlFlowGraph controlFlowGraph)
        {
            Signature = signature ?? throw new ArgumentNullException(nameof(signature));
            ControlFlowGraph = controlFlowGraph ?? throw new ArgumentNullException(nameof(controlFlowGraph));
            DominatorInfo = new DominatorInfo(controlFlowGraph.Entrypoint);
            DominatorTree = DominatorInfo.ToDominatorTree();

            for (int i = 0; i < signature.ParameterTokens.Count; i++)
                Parameters.Add(new ILParameter("arg_" + i, i));
        }

        public IList<ILParameter> Parameters
        {
            get;
        } = new List<ILParameter>();
        
        public ICollection<ILVariable> Variables => _variables.Values;

        public VMFunctionSignature Signature
        {
            get;
        }

        public ControlFlowGraph ControlFlowGraph
        {
            get;
        }

        public DominatorInfo DominatorInfo
        {
            get;
        }

        public Graph DominatorTree
        {
            get;
        }
        
        public ILVariable GetOrCreateVariable(string name)
        {
            if (!_variables.TryGetValue(name, out var variable))
                _variables.Add(name, variable = new ILVariable(name));
            return variable;
        }

        public bool RemoveNonUsedVariables()
        {
            bool changed = false;
            foreach (var entry in _variables.ToArray())
            {
                if (entry.Value.UsedBy.Count == 0)
                {
                    _variables.Remove(entry.Key);
                    changed = true;
                }
            }

            return changed;
        }

        public override void ReplaceNode(ILAstNode node, ILAstNode newNode)
        {
            throw new NotSupportedException();
        }

        public override IEnumerable<ILAstNode> GetChildren()
        {
            return ControlFlowGraph.Nodes.Select(x => (ILAstBlock) x.UserData[ILAstBlock.AstBlockProperty]);
        }

        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitCompilationUnit(this);
        }
        
        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitCompilationUnit(this);
        }
    }
}