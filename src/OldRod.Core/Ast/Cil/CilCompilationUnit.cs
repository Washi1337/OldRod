namespace OldRod.Core.Ast.Cil
{
    public class CilCompilationUnit : CilAstNode
    {
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