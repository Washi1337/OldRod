namespace OldRod.Core.Ast.IL.Transform
{
    public interface IAstTransform
    {
        string Name { get; }
        
        void ApplyTransformation(ILCompilationUnit unit);
    }
}