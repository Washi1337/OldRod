using System.Collections.Generic;
using System.Linq;
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL
{
    public class ILPhiExpression : ILExpression
    {
        public ILPhiExpression(params ILVariableExpression[] variables)
            : this(variables.AsEnumerable())
        {
        }

        public ILPhiExpression(IEnumerable<ILVariableExpression> variables)
            : base(VMType.Object)
        {
            Variables = new AstNodeCollection<ILVariableExpression>(this);
            foreach (var variable in variables)
                Variables.Add(variable);
            ExpressionType = Variables[0].ExpressionType;
        }
        
        public override bool HasPotentialSideEffects => false;
        
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

        public override IEnumerable<ILAstNode> GetChildren()
        {
            return Variables;
        }

        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitPhiExpression(this);
        }

        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitPhiExpression(this);
        }

        public override string ToString()
        {
            return $"φ({string.Join(", ", Variables)})";
        }
    }
}