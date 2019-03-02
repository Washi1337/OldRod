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