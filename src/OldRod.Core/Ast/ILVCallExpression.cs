using System.Collections.Generic;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Ast
{
    public class ILVCallExpression : ILExpression, IArgumentsProvider
    {
        public ILVCallExpression(VCallMetadata metadata)
            : base(metadata.ReturnType)
        {
            Metadata = metadata;
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
        } = new List<ILExpression>();
        
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
            return $"{Call}({Metadata})";
        }
    }
}