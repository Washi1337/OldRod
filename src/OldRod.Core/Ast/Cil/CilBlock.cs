using System.Collections.Generic;

namespace OldRod.Core.Ast.Cil
{
    public class CilBlock : CilStatement
    {
        public CilBlock()
        {
            Statements = new AstNodeCollection<CilStatement>(this);
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

        public override void AcceptVisitor(ICilAstVisitor visitor)
        {
            visitor.VisitBlock(this);
        }

        public override TResult AcceptVisitor<TResult>(ICilAstVisitor<TResult> visitor)
        {
            return visitor.VisitBlock(this);
        }
    }
}