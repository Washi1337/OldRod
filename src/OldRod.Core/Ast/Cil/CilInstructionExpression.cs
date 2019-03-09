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
using System.Linq;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.Cil
{
    public class CilInstructionExpression : CilExpression
    {
        public CilInstructionExpression()
        {
            Instructions = new List<CilInstruction>();
            Arguments = new AstNodeCollection<CilExpression>(this);
        }
        
        public CilInstructionExpression(CilOpCode opCode)
            : this(opCode, null, Enumerable.Empty<CilExpression>())
        {
        }
       
        public CilInstructionExpression(CilOpCode opCode, object operand)
            : this(opCode, operand, Enumerable.Empty<CilExpression>())
        {
        }

        public CilInstructionExpression(CilOpCode opCode, object operand, params CilExpression[] arguments)
            : this(opCode, operand, arguments.AsEnumerable())
        {
        }

        public CilInstructionExpression(CilOpCode opCode, object operand, IEnumerable<CilExpression> arguments)
            : this(new[] {new CilInstruction(0, opCode, operand) }, arguments)
        {
        }

        public CilInstructionExpression(IEnumerable<CilInstruction> instructions, IEnumerable<CilExpression> arguments)
        {
            Instructions = new List<CilInstruction>(instructions);
            Arguments = new AstNodeCollection<CilExpression>(this);
            
            foreach (var argument in arguments)
                Arguments.Add(argument);   
        }

        public IList<CilInstruction> Instructions
        {
            get;
        }

        public IList<CilExpression> Arguments
        {
            get;
        }

        public VMFlags AffectedFlags
        {
            get;
            set;
        }

        public bool ShouldEmitFlagsUpdate
        {
            get;
            set;
        }
        
        public bool InvertedFlagsUpdate
        {
            get;
            set;
        }

        public override void ReplaceNode(CilAstNode node, CilAstNode newNode)
        {
            AssertNodeParents(node, newNode);
            int index = Arguments.IndexOf((CilExpression) node);
            
            if (newNode == null)
                Arguments.RemoveAt(index);
            else
                Arguments[index] = (CilExpression) newNode;
        }

        public override IEnumerable<CilAstNode> GetChildren()
        {
            return Arguments;
        }

        public override void AcceptVisitor(ICilAstVisitor visitor)
        {
            visitor.VisitInstructionExpression(this);
        }

        public override TResult AcceptVisitor<TResult>(ICilAstVisitor<TResult> visitor)
        {
            return visitor.VisitInstructionExpression(this);
        }

        public override string ToString()
        {
            string instructionsString = string.Join(" - ", Instructions.Select(i => i.Operand == null
                ? i.OpCode.Name
                : i.OpCode.Name + " " + i.Operand));

            return Arguments.Count == 0
                ? instructionsString
                : $"{instructionsString}({string.Join(", ", Arguments)})";
        }
        
    }
}