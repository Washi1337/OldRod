// Project OldRod - A KoiVM devirtualisation utility.
// Copyright (C) 2019 Washi
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
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

            if (newNode == null)
                Statements.RemoveAt(index);
            else
                Statements[index] = (ILStatement) newNode;
        }

        public override IEnumerable<ILAstNode> GetChildren()
        {
            return Statements;
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