using System.Collections.Generic;

namespace OldRod.Core.Ast.IL.Pattern
{
    public class ILSequencePattern<TNode> 
        where TNode : ILAstNode
    {
        public ILSequencePattern(params ILAstPattern[] patterns)
        {
            Sequence = new List<ILAstPattern>(patterns);
        }
        
        public IList<ILAstPattern> Sequence
        {
            get;
        }
        
        public MatchResult Match(IList<TNode> nodes, int start = 0)
        {
            var result = new MatchResult(start < nodes.Count 
                && start + Sequence.Count < nodes.Count);

            for (int i = 0; result.Success && i < Sequence.Count; i++)
                result.CombineWith(Sequence[i].Match(nodes[i + start]));

            return result;
        }

        public MatchResult FindMatch(IList<TNode> nodes)
        {
            for (int i = 0; i < nodes.Count - Sequence.Count; i++)
            {
                var result = Match(nodes, i);
                if (result.Success)
                    return result;
            }
            
            return new MatchResult(false);
        }

        public override string ToString()
        {
            return string.Join(" -> ", Sequence);
        }
    }
}