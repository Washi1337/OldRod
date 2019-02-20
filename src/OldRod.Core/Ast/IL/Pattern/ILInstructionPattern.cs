using System.Collections.Generic;

namespace OldRod.Core.Ast.IL.Pattern
{
    public class ILInstructionPattern : ILExpressionPattern
    {
        public static ILInstructionPattern Any() => new ILInstructionAnyPattern();
        
        private sealed class ILInstructionAnyPattern : ILInstructionPattern
        {
            public ILInstructionAnyPattern() 
                : base(ILOpCodePattern.Any(), ILOperandPattern.Any())
            {
            }
            
            public override MatchResult Match(ILAstNode node)
            {
                var result = new MatchResult(node is ILInstructionExpression);
                AddCaptureIfNecessary(result, node);
                return result;
            }
        }
        
        public ILInstructionPattern(ILOpCodePattern opCode, ILOperandPattern operand, params ILExpressionPattern[] arguments)
        {
            OpCode = opCode;
            Operand = operand;
            Arguments = new List<ILExpressionPattern>(arguments);
        }
        
        public ILOpCodePattern OpCode
        {
            get;
            set;
        }

        public ILOperandPattern Operand
        {
            get;
            set;
        }

        public IList<ILExpressionPattern> Arguments
        {
            get;
        }

        public override MatchResult Match(ILAstNode node)
        {
            var result = new MatchResult(false);

            if (node is ILInstructionExpression expression)
            {
                result.Success = OpCode.Match(expression.OpCode.Code)
                                 && Operand.Match(expression.Operand)
                                 && expression.Arguments.Count == Arguments.Count;

                for (int i = 0; result.Success && i < expression.Arguments.Count; i++)
                {
                    var argumentMatch = Arguments[i].Match(expression.Arguments[i]);
                    result.CombineWith(argumentMatch);
                }
            }

            AddCaptureIfNecessary(result, node);
            return result;
        }
    }
}