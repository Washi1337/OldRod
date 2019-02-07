namespace OldRod.Core.Ast.Transform
{
    public interface IAstTransform
    {
        void ApplyTransformation(ILCompilationUnit unit);
    }
}