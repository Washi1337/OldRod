namespace OldRod.Core.Ast.IL.Pattern
{
    public class ILAssignmentPattern : ILStatementPattern
    {
        public ILAssignmentPattern(ILVariablePattern variable, ILExpressionPattern value)
        {
            Variable = variable;
            Value = value;
        }
        
        public ILVariablePattern Variable
        {
            get;
        }

        public ILExpressionPattern Value
        {
            get;
        }
        
        public override MatchResult Match(ILAstNode node)
        {
            var result = new MatchResult(false);

            if (node is ILAssignmentStatement statement)
            {
                result.Success = Variable.VariableName == statement.Variable.Name;
                if (result.Success) 
                    result.CombineWith(Value.Match(statement.Value));
            }

            AddCaptureIfNecessary(result, node);
            return result;
        }
    }
}