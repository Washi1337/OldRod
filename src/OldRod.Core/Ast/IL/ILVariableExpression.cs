using System;

namespace OldRod.Core.Ast.IL
{
    public class ILVariableExpression : ILExpression
    {
        private ILVariable _variable;

        public ILVariableExpression(ILVariable variable) 
            : base(variable.VariableType)
        {
            Variable = variable;
        }

        public ILVariable Variable
        {
            get => _variable;
            set
            {
                _variable?.UsedBy.Remove(this);
                _variable = value;
                value?.UsedBy.Add(this);
            }
        }
        
        public override bool HasPotentialSideEffects => false;

        public override string ToString()
        {
            return Variable.Name;
        }

        public override void ReplaceNode(ILAstNode node, ILAstNode newNode)
        {
            throw new InvalidOperationException();
        }

        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitVariableExpression(this);
        }
        
        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitVariableExpression(this);
        }

    }
}