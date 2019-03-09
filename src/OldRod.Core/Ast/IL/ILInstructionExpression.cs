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
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL
{
    public class ILInstructionExpression : ILExpression, IILArgumentsProvider
    {
        public ILInstructionExpression(ILInstruction instruction)
            : this(instruction.Offset, instruction.OpCode, instruction.Operand, instruction.OpCode.StackBehaviourPush.GetResultType())
        {
        }

        public ILInstructionExpression(int originalOffset, ILOpCode opCode, object operand, VMType type)
            : base(type)
        {
            OriginalOffset = originalOffset;
            OpCode = opCode;
            Operand = operand;
            Arguments = new AstNodeCollection<ILExpression>(this);
        }

        public override bool HasPotentialSideEffects
        {
            get
            {
                if (IsFlagDataSource)
                    return true;
                
                switch (OpCode.Code)
                {
                    case ILCode.CALL:
                        return true;
                    
                    default:
                        return Arguments.Any(x => x.HasPotentialSideEffects);
                }
            }
        }
        
        public int OriginalOffset
        {
            get;
            set;
        }

        public ILOpCode OpCode
        {
            get;
            set;
        }

        public object Operand
        {
            get;
            set;
        }
        
        public IList<ILExpression> Arguments
        {
            get;
        }

        public override string ToString()
        {
            if (Operand == null)
                return $"{OpCode}({string.Join(", ", Arguments)})";
            if (Arguments.Count == 0)
                return OpCode + "(" + Operand + ")";
            return $"{OpCode}({Operand} : {string.Join(", ", Arguments)})";
        }

        public override void ReplaceNode(ILAstNode node, ILAstNode newNode)
        {
            AssertNodeParents(node, newNode);
            int index = Arguments.IndexOf((ILExpression) node);
            
            if (newNode == null)
                Arguments.RemoveAt(index);
            else
                Arguments[index] = (ILExpression) newNode;
        }

        public override IEnumerable<ILAstNode> GetChildren()
        {
            return Arguments;
        }

        public override void AcceptVisitor(IILAstVisitor visitor)
        {
            visitor.VisitInstructionExpression(this);
        }
        
        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitInstructionExpression(this);
        }
    }
}