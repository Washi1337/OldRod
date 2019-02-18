using System.Collections.Generic;

namespace OldRod.Core.Ast.IL
{
    public class ILPhiExpression : ILExpression
    {
        public ILPhiExpression(params ILVariableExpression[] variables)
            : base(variables[0].ExpressionType)
        {
            Variables = new AstNodeCollection<ILVariableExpression>(this);
            foreach (var variable in variables)
                Variables.Add(variable);
        }

        public IList<ILVariableExpression> Variables
        {
            get;
        }

        public override void ReplaceNode(ILAstNode node, ILAstNode newNode)
        {
            AssertNodeParents(node, newNode);
            int index = Variables.IndexOf((ILVariableExpression) node);
            
            if (newNode == null)
                Variables.RemoveAt(index);
            else
                Variables[index] = (ILVariableExpression) newNode;
        }

        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitPhiExpression(this);
        }

        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitPhiExpression(this);
        }
    }
}