using System.Collections.Generic;

namespace OldRod.Core.Ast
{
    public class ILAstBlock : ILAstNode
    {
        public const string AstBlockProperty = "astblock";
        
        public IList<ILStatement> Statements
        {
            get;
        } = new List<ILStatement>();
        
        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitBlock(this);
        }

        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitBlock(this);
        }
    }
}