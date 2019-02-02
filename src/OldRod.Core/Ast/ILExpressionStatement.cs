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
    }
}