using System.Collections.Generic;

namespace OldRod.Core.Ast.IL.Pattern
{
    public class MatchResult
    {
        public MatchResult()
            : this(true)
        {
        }
        
        public MatchResult(bool success)
        {
            Success = success;
        }

        public bool Success
        {
            get;
            set;
        }

        public IDictionary<string, ICollection<ILAstNode>> Captures
        {
            get;
        } = new Dictionary<string, ICollection<ILAstNode>>();

        public void AddCapture(string name, ILAstNode node)
        {
            if (!Captures.TryGetValue(name, out var captures))
                Captures.Add(name, captures = new List<ILAstNode>());

            captures.Add(node);
        }
        
        public void CombineWith(MatchResult result)
        {
            if (!result.Success)
                Success = false;

            foreach (var entry in result.Captures)
            {
                if (!Captures.TryGetValue(entry.Key, out var captures))
                    Captures.Add(entry.Key, captures = new List<ILAstNode>());

                foreach (var capture in entry.Value)
                    captures.Add(capture);
            }
        }

    }
}