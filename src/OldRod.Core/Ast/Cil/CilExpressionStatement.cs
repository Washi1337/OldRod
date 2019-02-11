using System;

namespace OldRod.Core.Ast.Cil
{
    public class CilExpressionStatement : CilStatement
    {
        private CilExpression _expression;

        public CilExpressionStatement(CilExpression expression)
        {
            Expression = expression;
        }
        
        public CilExpression Expression
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
        
        public override void ReplaceNode(CilAstNode node, CilAstNode newNode)
        {
            AssertNodeParents(node, newNode);
            Expression = (CilExpression) newNode;
        }

        public override void AcceptVisitor(ICilAstVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }

        public override TResult AcceptVisitor<TResult>(ICilAstVisitor<TResult> visitor)
        {
            return visitor.VisitExpressionStatement(this);
        }

        public override string ToString()
        {
            return Expression.ToString();
        }
    }
}