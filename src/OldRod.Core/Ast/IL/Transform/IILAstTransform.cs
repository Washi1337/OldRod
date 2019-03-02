namespace OldRod.Core.Ast.IL.Transform
{
    public interface IILAstTransform
    {
        string Name { get; }
        
        void ApplyTransformation(ILCompilationUnit unit, ILogger logger);
    }

    public interface IChangeAwareILAstTransform : IILAstTransform
    {
        new bool ApplyTransformation(ILCompilationUnit unit, ILogger logger);
    }
}