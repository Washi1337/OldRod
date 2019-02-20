namespace OldRod.Core.Ast.IL.Pattern
{
    public class ILExpressionStatementPattern : ILStatementPattern
    {
        public static implicit operator ILExpressionStatementPattern(ILExpressionPattern expressionPattern)
        {
            return new ILExpressionStatementPattern(expressionPattern);
        }
        
        public ILExpressionStatementPattern(ILExpressionPattern expression)
        {
            Expression = expression;
        }
        
        public ILExpressionPattern Expression
        {
            get;
        }

        public override MatchResult Match(ILAstNode node)
        {
            var result = new MatchResult(false);
            if (node is ILExpressionStatement statement)
            {
                result.Success = true;
                result.CombineWith(Expression.Match(statement.Expression));
            }

            AddCaptureIfNecessary(result, node);
            return result;
        }

        public override string ToString()
        {
            return Expression.ToString();
        }
    }
}