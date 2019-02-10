namespace OldRod.Core.Ast.Cil
{
    public interface ICilAstVisitor
    {
        void VisitCompilationUnit(CilCompilationUnit unit);
        void VisitBlock(CilBlock block);
        void VisitExpressionStatement(CilExpressionStatement statement);
        void VisitInstructionExpression(CilExpression expression);
    }

    public interface ICilAstVisitor<out TResult>
    {
        TResult VisitCompilationUnit(CilCompilationUnit unit);
        TResult VisitBlock(CilBlock block);
        TResult VisitExpressionStatement(CilExpressionStatement statement);
        TResult VisitInstructionExpression(CilExpression expression);
    }
}