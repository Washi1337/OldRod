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

using System.Collections.Generic;
using AsmResolver.Net.Cil;

namespace OldRod.Core.Ast.Cil
{
    public class CilAstBlock : CilStatement
    {
        public const string AstBlockProperty = "cilastblock";

        public CilAstBlock()
        {
            Statements = new AstNodeCollection<CilStatement>(this);
            BlockHeader = CilInstruction.Create(CilOpCodes.Nop);
        }

        public CilInstruction BlockHeader
        {
            get;
        }
        
        public IList<CilStatement> Statements
        {
            get;
        }

        public override void ReplaceNode(CilAstNode node, CilAstNode newNode)
        {            
            AssertNodeParents(node, newNode);
            int index = Statements.IndexOf((CilStatement) node);
            Statements[index] = (CilStatement) newNode;
        }

        public override IEnumerable<CilAstNode> GetChildren()
        {
            return Statements;
        }

        public override void AcceptVisitor(ICilAstVisitor visitor)
        {
            visitor.VisitBlock(this);
        }

        public override TResult AcceptVisitor<TResult>(ICilAstVisitor<TResult> visitor)
        {
            return visitor.VisitBlock(this);
        }

        public override string ToString()
        {
            return string.Join("\n", Statements);
        }
    }
}