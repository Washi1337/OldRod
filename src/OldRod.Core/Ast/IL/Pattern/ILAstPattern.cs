namespace OldRod.Core.Ast.IL.Pattern
{
    public abstract class ILAstPattern
    {
        public bool Captured
        {
            get;
            protected set;
        }

        public string CaptureName
        {
            get;
            protected set;
        }

        public abstract MatchResult Match(ILAstNode node);

        public virtual ILAstPattern Capture(string name)
        {
            Captured = true;
            CaptureName = name;
            return this;
        }

        protected void AddCaptureIfNecessary(MatchResult result, ILAstNode node)
        {
            if (result.Success && Captured)
                result.AddCapture(CaptureName, node);
        }
    }
}