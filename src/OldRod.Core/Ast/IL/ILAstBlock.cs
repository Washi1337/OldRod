using System;
using System.Collections.Generic;
using Rivers;

namespace OldRod.Core.Ast.IL
{
    public class ILAstBlock : ILAstNode
    {
        public const string AstBlockProperty = "ilastblock";

        public ILAstBlock(Node cfgNode)
        {
            CfgNode = cfgNode ?? throw new ArgumentNullException(nameof(cfgNode));
            Statements = new AstNodeCollection<ILStatement>(this);
        }

        public Node CfgNode
        {
            get;
        }
        
        public IList<ILStatement> Statements
        {
            get;
        }

        public override void ReplaceNode(ILAstNode node, ILAstNode newNode)
        {
            AssertNodeParents(node, newNode);
            int index = Statements.IndexOf((ILStatement) node);
            Statements[index] = (ILStatement) newNode;
        }

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