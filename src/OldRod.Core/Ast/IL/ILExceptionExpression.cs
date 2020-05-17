using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL
{
    public class ILExceptionExpression : ILExpression
    {
        public ILExceptionExpression(ITypeDefOrRef exceptionType) 
            : base(VMType.Object)
        {
            ExceptionType = exceptionType;
        }

        public override bool HasPotentialSideEffects => true;

        public ITypeDefOrRef ExceptionType
        {
            get;
        }

        public override void ReplaceNode(ILAstNode node, ILAstNode newNode)
        {
            throw new NotSupportedException();
        }

        public override IEnumerable<ILAstNode> GetChildren()
        {
            return Enumerable.Empty<ILAstNode>();
        }

        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitExceptionExpression(this);
        }

        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitExceptionExpression(this);
        }

        public override string ToString()
        {
            return "PUSH_EXCEPTION";
        }
        
    }
}