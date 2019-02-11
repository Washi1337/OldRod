namespace OldRod.Core.Ast.Cil
{
    public interface ICilAstVisitor
    {
        void VisitCompilationUnit(CilCompilationUnit unit);
        void VisitBlock(CilAstBlock block);
        void VisitExpressionStatement(CilExpressionStatement statement);
        void VisitInstructionExpression(CilInstructionExpression expression);
    }

    public interface ICilAstVisitor<out TResult>
    {
        TResult VisitCompilationUnit(CilCompilationUnit unit);
        TResult VisitBlock(CilAstBlock block);
        TResult VisitExpressionStatement(CilExpressionStatement statement);
        TResult VisitInstructionExpression(CilInstructionExpression expression);
    }
}