using System;

namespace OldRod.Core.Ast.IL
{
    public class ILAssignmentStatement : ILStatement
    {
        private ILExpression _value;

        public ILAssignmentStatement(ILVariable variable, ILExpression value)
        {
            Variable = variable;
            Value = value;
        }
        
        public ILVariable Variable
        {
            get;
            set;
        }

        public ILExpression Value
        {
            get => _value;
            set
            {
                if (value?.Parent != null)
                    throw new ArgumentException("Item is already added to another node.");
                if (_value != null)
                    _value.Parent = null;
                _value = value;
                if (value != null)
                    value.Parent = this;
            }
        }

        public override void ReplaceNode(ILAstNode node, ILAstNode newNode)
        {
            AssertNodeParents(node, newNode);
            Value = (ILExpression) newNode;
        }


        public override string ToString()
        {
            return $"{Variable.Name} = {Value}";
        }

        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitAssignmentStatement(this);
        }

        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitAssignmentStatement(this);
        }
    }
}