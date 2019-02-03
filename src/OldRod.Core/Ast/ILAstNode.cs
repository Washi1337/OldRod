namespace OldRod.Core.Ast
{
    public abstract class ILAstNode
    {
        public abstract void AcceptVisitor(IILAstVisitor visitor);
        
        public abstract TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor);
    }
}