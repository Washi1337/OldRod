using System.Collections.Generic;

namespace OldRod.Core.Ast.IL
{
    public class VariableUsageCollector : IILAstVisitor<IEnumerable<ILVariableExpression>>
    {
        public IEnumerable<ILVariableExpression> VisitCompilationUnit(ILCompilationUnit unit)
        {
            var result = new List<ILVariableExpression>();

            foreach (var node in unit.ControlFlowGraph.Nodes)
            {
                var block = (ILAstBlock) node.UserData[ILAstBlock.AstBlockProperty];
                result.AddRange(block.AcceptVisitor(this));
            }

            return result;
        }

        public IEnumerable<ILVariableExpression> VisitBlock(ILAstBlock block)
        {
            var result = new List<ILVariableExpression>();
            foreach (var statement in block.Statements)
                result.AddRange(statement.AcceptVisitor(this));
            return result;
        }

        public IEnumerable<ILVariableExpression> VisitExpressionStatement(ILExpressionStatement statement)
        {
            return statement.Expression.AcceptVisitor(this);
        }

        public IEnumerable<ILVariableExpression> VisitAssignmentStatement(ILAssignmentStatement statement)
        {
            return statement.Value.AcceptVisitor(this);
        }

        public IEnumerable<ILVariableExpression> VisitInstructionExpression(ILInstructionExpression expression)
        {
            var result = new List<ILVariableExpression>();
            foreach (var argument in expression.Arguments)
                result.AddRange(argument.AcceptVisitor(this));
            return result;
        }

        public IEnumerable<ILVariableExpression> VisitVariableExpression(ILVariableExpression expression)
        {
            return new[] {expression};
        }

        public IEnumerable<ILVariableExpression> VisitVCallExpression(ILVCallExpression expression)
        {
            var result = new List<ILVariableExpression>();
            foreach (var argument in expression.Arguments)
                result.AddRange(argument.AcceptVisitor(this));
            return result;
        }
    }
}