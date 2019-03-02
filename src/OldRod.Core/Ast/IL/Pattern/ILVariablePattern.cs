using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL.Pattern
{
    public class ILVariablePattern : ILExpressionPattern
    {
        public static implicit operator ILVariablePattern(string variableName)
        {
            return new ILVariablePattern(variableName);
        }
        
        public static implicit operator ILVariablePattern(VMRegisters register)
        {
            return new ILVariablePattern(register);
        }
        
        public new static readonly ILVariablePattern Any = new ILVariableAnyPattern();
        
        private sealed class ILVariableAnyPattern : ILVariablePattern
        {
            public ILVariableAnyPattern() 
                : base(null)
            {
            }

            public override MatchResult Match(ILAstNode node)
            {
                var result = new MatchResult(node is ILVariableExpression);
                AddCaptureIfNecessary(result, node);
                return result;
            }

            public override ILAstPattern Capture(string name)
            {
                var result = new ILVariableAnyPattern
                {
                    Captured = true,
                    CaptureName = name
                };
                return result;
            }

            public override string ToString()
            {
                return "?";
            }
        }

        public ILVariablePattern(string variableName)
        {
            VariableName = variableName;
        }
        
        public ILVariablePattern(VMRegisters register)
        {
            VariableName = register.ToString();
        }

        public string VariableName
        {
            get;
        }
        
        public override MatchResult Match(ILAstNode node)
        {
            var result = new MatchResult(node is ILVariableExpression expression
                                         && expression.Variable.Name == VariableName);
            AddCaptureIfNecessary(result, node);
            return result;
        }

        public ILVariablePattern CaptureVar(string name)
        {
            return (ILVariablePattern) Capture(name);
        }

        public override string ToString()
        {
            return VariableName;
        }
    }
}