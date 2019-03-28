using System;
using System.Collections.Generic;
using AsmResolver.Net.Cts;

namespace OldRod.Core.Ast.Cil
{
    public class CilUnboxToVmExpression : CilExpression
    {
        private CilExpression _expression;

        public CilUnboxToVmExpression(ITypeDefOrRef type, CilExpression expression)
        {
            Type = type;
            Expression = expression;
        }
        
        public ITypeDefOrRef Type
        {
            get;
            set;
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

        public override IEnumerable<CilAstNode> GetChildren()
        {
            return new[] {Expression};
        }

        public override void AcceptVisitor(ICilAstVisitor visitor)
        {
            visitor.VisitUnboxToVmExpression(this);
        }

        public override TResult AcceptVisitor<TResult>(ICilAstVisitor<TResult> visitor)
        {
            return visitor.VisitUnboxToVmExpression(this);
        }
    }
}