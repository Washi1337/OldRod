using System;
using System.Collections.Generic;

namespace OldRod.Core.Ast.IL
{
    public class ILAssignmentStatement : ILStatement
    {
        private ILExpression _value;
        private ILVariable _variable;

        public ILAssignmentStatement(ILVariable variable, ILExpression value)
        {
            Variable = variable;
            Value = value;
        }

        public ILVariable Variable
        {
            get => _variable;
            set
            {
                _variable?.AssignedBy.Remove(this);
                _variable = value;
                value?.AssignedBy.Add(this);
            }
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

        public override IEnumerable<ILAstNode> GetChildren()
        {
            throw new NotImplementedException();
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