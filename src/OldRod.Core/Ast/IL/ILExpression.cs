using System.Collections.Generic;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL
{
    public abstract class ILExpression : ILAstNode
    {
        protected ILExpression(VMType expressionType)
        {
            ExpressionType = expressionType;
        }
        
        public VMType ExpressionType
        {
            get;
            set;
        }
    }
}