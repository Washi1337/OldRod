namespace OldRod.Core.Ast
{
    public class ILVariableExpression : ILExpression
    {
        public ILVariableExpression(ILVariable variable)
        {
            Variable = variable;
        }
        
        public ILVariable Variable
        {
            get;
        }

        public override string ToString()
        {
            return Variable.Name;
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