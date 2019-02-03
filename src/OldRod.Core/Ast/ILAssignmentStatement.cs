namespace OldRod.Core.Ast
{
    public class ILAssignmentStatement : ILStatement
    {
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
            get;
            set;
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