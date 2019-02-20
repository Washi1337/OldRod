namespace OldRod.Core.Ast.IL.Pattern
{
    public abstract class ILStatementPattern : ILAstPattern
    {
        public static ILStatementPattern Any() => new ILStatementAnyPattern();
        
        private sealed class ILStatementAnyPattern : ILStatementPattern
        {
            public override MatchResult Match(ILAstNode node)
            {
                var result = new MatchResult(node is ILStatement);
                AddCaptureIfNecessary(result, node);
                return result;
            }

            public override string ToString()
            {
                return "?";
            }
        }
    }
}