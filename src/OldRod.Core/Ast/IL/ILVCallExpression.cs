using System;
using System.Collections.Generic;
using System.Linq;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Ast.IL
{
    public class ILVCallExpression : ILExpression, IILArgumentsProvider
    {
        public ILVCallExpression(VCallMetadata metadata)
            : base(metadata.ReturnType)
        {
            Metadata = metadata;
            Arguments = new AstNodeCollection<ILExpression>(this);
        }

        public override bool HasPotentialSideEffects
        {
            get
            {
                switch (Call)
                {
                    case VMCalls.UNBOX:
                    case VMCalls.BOX:
                    case VMCalls.CAST:
                    case VMCalls.SIZEOF:
                        return Arguments.Any(x => x.HasPotentialSideEffects);
                    
                    default:
                        return true;
                }
            }
        }

        public VMCalls Call => Metadata.VMCall;
        
        public VCallMetadata Metadata
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
                ? $"{Call}({Metadata})"
                : $"{Call}({Metadata} : {string.Join(", ", Arguments)})";
        }
    }
}