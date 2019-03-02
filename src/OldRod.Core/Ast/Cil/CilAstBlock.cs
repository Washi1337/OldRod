using System.Collections.Generic;
using AsmResolver.Net.Cil;

namespace OldRod.Core.Ast.Cil
{
    public class CilAstBlock : CilStatement
    {
        public const string AstBlockProperty = "cilastblock";

        public CilAstBlock()
        {
            Statements = new AstNodeCollection<CilStatement>(this);
            BlockHeader = CilInstruction.Create(CilOpCodes.Nop);
        }

        public CilInstruction BlockHeader
        {
            get;
        }
        
        public IList<CilStatement> Statements
        {
            get;
        }

        public override void ReplaceNode(CilAstNode node, CilAstNode newNode)
        {            
            AssertNodeParents(node, newNode);
            int index = Statements.IndexOf((CilStatement) node);
            Statements[index] = (CilStatement) newNode;
        }

        public override IEnumerable<CilAstNode> GetChildren()
        {
            return Statements;
        }

        public override void AcceptVisitor(ICilAstVisitor visitor)
        {
            visitor.VisitBlock(this);
        }

        public override TResult AcceptVisitor<TResult>(ICilAstVisitor<TResult> visitor)
        {
            return visitor.VisitBlock(this);
        }

        public override string ToString()
        {
            return string.Join("\n", Statements);
        }
    }
}