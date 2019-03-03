using System.Linq;

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

    public abstract class ChangeAwareILAstTransform : IChangeAwareILAstTransform, IILAstVisitor<bool>
    {
        public abstract string Name
        {
            get;
        }

        void IILAstTransform.ApplyTransformation(ILCompilationUnit unit, ILogger logger)
        {
            ApplyTransformation(unit, logger);
        }

        public bool ApplyTransformation(ILCompilationUnit unit, ILogger logger)
        {
            bool changed = false;
            while (unit.AcceptVisitor(this))
            {
                changed = true;
                // Repeat until no more changes.
            }

            return changed;
        }

        public virtual bool VisitCompilationUnit(ILCompilationUnit unit)
        {
            bool changed = false;
            foreach (var node in unit.ControlFlowGraph.Nodes)
            {
                var block = (ILAstBlock) node.UserData[ILAstBlock.AstBlockProperty];
                changed |= block.AcceptVisitor(this);
            }

            return changed;
        }

        public virtual bool VisitBlock(ILAstBlock block)
        {
            bool changed = false;
            foreach (var statement in block.Statements)
                changed |= statement.AcceptVisitor(this);
            return changed;
        }

        public virtual bool VisitExpressionStatement(ILExpressionStatement statement)
        {
            return statement.Expression.AcceptVisitor(this);
        }

        public virtual bool VisitAssignmentStatement(ILAssignmentStatement statement)
        {
            return statement.Value.AcceptVisitor(this);
        }

        public virtual bool VisitInstructionExpression(ILInstructionExpression expression)
        {
            return TryOptimiseArguments(expression);
        }

        public virtual bool VisitVariableExpression(ILVariableExpression expression)
        {
            return false;
        }

        public virtual bool VisitVCallExpression(ILVCallExpression expression)
        {
            return TryOptimiseArguments(expression);
        }

        public virtual bool VisitPhiExpression(ILPhiExpression expression)
        {
            return false;
        }

        private bool TryOptimiseArguments(IILArgumentsProvider provider)
        {
            bool changed = false;
            foreach (var argument in provider.Arguments.ToArray())
                changed |= argument.AcceptVisitor(this);
            return changed;
        }
    }
}