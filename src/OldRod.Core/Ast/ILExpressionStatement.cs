namespace OldRod.Core.Ast
{
    public class ILExpressionStatement : ILStatement
    {
        public ILExpressionStatement(ILExpression expression)
        {
            Expression = expression;
        }
        
        public ILExpression Expression
        {
            get;
        }

        public override string ToString()
        {
            return Expression.ToString();
        }

        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }
        
        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitExpressionStatement(this);
        }
    }
}