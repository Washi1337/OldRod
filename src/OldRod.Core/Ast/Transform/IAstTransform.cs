namespace OldRod.Core.Ast.Transform
{
    public interface IAstTransform
    {
        string Name { get; }
        
        void ApplyTransformation(ILCompilationUnit unit);
    }
}