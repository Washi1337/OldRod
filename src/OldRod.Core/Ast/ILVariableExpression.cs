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
    }
}