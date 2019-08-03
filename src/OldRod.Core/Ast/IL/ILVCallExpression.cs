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
using AsmResolver.Net;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Ast.IL
{
    public class ILVCallExpression : ILExpression, IILArgumentsProvider
    {
        public ILVCallExpression(VCallAnnotation annotation)
            : base(annotation.ReturnType)
        {
            Annotation = annotation;
            Arguments = new AstNodeCollection<ILExpression>(this);
        }

        public override bool HasPotentialSideEffects
        {
            get
            {
                switch (Call)
                {
                    case VMCalls.BOX:
                        var boxAnnotation = (BoxAnnotation) Annotation;
                        if (boxAnnotation.Type.IsTypeOf("System", "String") && !boxAnnotation.IsUnknownValue)
                            return false;
                        return Arguments.Any(x => x.HasPotentialSideEffects);
                    
                    case VMCalls.UNBOX:
                    case VMCalls.CAST:
                    case VMCalls.SIZEOF:
                    case VMCalls.TOKEN:
                    case VMCalls.LDFLD:
                    case VMCalls.LDFTN:
                        return Arguments.Any(x => x.HasPotentialSideEffects);
                    
                    default:
                        return true;
                }
            }
        }

        public VMCalls Call => Annotation.VMCall;
        
        public VCallAnnotation Annotation
        {
            get;
            set;
        }

        public IList<ILExpression> Arguments
        {
            get;
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
            visitor.VisitVCallExpression(this);
        }

        public override TResult AcceptVisitor<TResult>(IILAstVisitor<TResult> visitor)
        {
            return visitor.VisitVCallExpression(this);
        }

        public override string ToString()
        {
            return Arguments.Count == 0
                ? $"{Call}({Annotation})"
                : $"{Call}({Annotation} : {string.Join(", ", Arguments)})";
        }
    }
}