using System.Collections.Generic;
using AsmResolver.Net.Signatures;
using OldRod.Core.Disassembly.ControlFlow;

namespace OldRod.Core.Ast.Cil
{
    public class CilCompilationUnit : CilAstNode
    {
        public CilCompilationUnit(ControlFlowGraph graph)
        {
            ControlFlowGraph = graph;
        }
        
        public ICollection<VariableSignature> Variables
        {
            get;
        } = new List<VariableSignature>();
        
        public ControlFlowGraph ControlFlowGraph
        {
            get;
        }
        
        public override void ReplaceNode(CilAstNode node, CilAstNode newNode)
        {
            throw new System.NotImplementedException();
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