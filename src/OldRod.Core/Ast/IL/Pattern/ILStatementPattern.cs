namespace OldRod.Core.Ast.IL.Pattern
{
    public abstract class ILStatementPattern : ILAstPattern
    {
        public static readonly ILStatementPattern Any = new ILStatementAnyPattern();
        
        private sealed class ILStatementAnyPattern : ILStatementPattern
        {
            public override MatchResult Match(ILAstNode node)
            {
                var result = new MatchResult(node is ILStatement);
                AddCaptureIfNecessary(result, node);
                return result;
            }

            public override ILAstPattern Capture(string name)
            {
                return new ILStatementAnyPattern
                {
                    Captured = true,
                    CaptureName = name
                };
            }

            public override string ToString()
            {
                return "?";
            }
        }
    }
}