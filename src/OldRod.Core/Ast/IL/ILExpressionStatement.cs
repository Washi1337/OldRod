using System;

namespace OldRod.Core.Ast.IL
{
    public class ILExpressionStatement : ILStatement
    {
        private ILExpression _expression;

        public ILExpressionStatement(ILExpression expression)
        {
            _expression = expression;
        }

        public ILExpression Expression
        {
            get => _expression;
            set
            {
                if (value?.Parent != null)
                    throw new ArgumentException("Item is already added to another node.");
                if (_expression != null)
                    _expression.Parent = null;
                _expression = value;
                if (value != null)
                    value.Parent = this;
            }
        }

        public override string ToString()
        {
            return Expression.ToString();
        }

        public override void ReplaceNode(ILAstNode node, ILAstNode newNode)
        {
            AssertNodeParents(node, newNode);
            Expression = (ILExpression) newNode;
        }

        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }
        
        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitExpressionStatement(this);
        }
    }
}