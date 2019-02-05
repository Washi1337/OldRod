namespace OldRod.Core.Ast
{
    public interface IILAstVisitor
    {
        void VisitCompilationUnit(ILCompilationUnit unit);
        void VisitBlock(ILAstBlock block);
        void VisitExpressionStatement(ILExpressionStatement statement);
        void VisitAssignmentStatement(ILAssignmentStatement statement);
        void VisitInstructionExpression(ILInstructionExpression expression);
        void VisitVariableExpression(ILVariableExpression expression);
        void VisitVCallExpression(ILVCallExpression expression);
    }
    
    public interface IILAstVisitor<TResult>
    {
        TResult VisitCompilationUnit(ILCompilationUnit unit);
        TResult VisitBlock(ILAstBlock block);
        TResult VisitExpressionStatement(ILExpressionStatement statement);
        TResult VisitAssignmentStatement(ILAssignmentStatement statement);
        TResult VisitInstructionExpression(ILInstructionExpression expression);
        TResult VisitVariableExpression(ILVariableExpression expression);
        TResult VisitVCallExpression(ILVCallExpression expression);
    }
}