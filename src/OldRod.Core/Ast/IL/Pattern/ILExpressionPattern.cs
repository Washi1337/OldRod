namespace OldRod.Core.Ast.IL.Pattern
{
    public abstract class ILExpressionPattern : ILAstPattern
    {
        public static ILExpressionPattern Any => new ILExpressionAnyPattern();
        
        private sealed class ILExpressionAnyPattern : ILExpressionPattern
        {
            public override MatchResult Match(ILAstNode node)
            {
                var result = new MatchResult(node is ILExpression);
                AddCaptureIfNecessary(result, node);
                return result;
            }

            public override ILAstPattern Capture(string name)
            {
                return new ILExpressionAnyPattern
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