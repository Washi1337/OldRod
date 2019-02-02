using System.Collections.Generic;
using System.Linq;

namespace OldRod.Core.Disassembly.Inference
{
    public class JumpMetadata : InferredMetadata
    {
        public JumpMetadata()
            : this(Enumerable.Empty<ulong>())
        {
        }

        public JumpMetadata(params ulong[] targets)
            : this(targets.AsEnumerable())
        {    
        }
        
        public JumpMetadata(IEnumerable<ulong> inferredJumpTargets)
        {
            InferredJumpTargets = new List<ulong>(inferredJumpTargets);
        }

        public IList<ulong> InferredJumpTargets
        {
            get;
        }

        public override string ToString()
        {
            return InferredJumpTargets.Count == 1
                ? "Jump to " + InferredJumpTargets[0].ToString("X4")
                : $"Jump to one of {{{string.Join(", ", InferredJumpTargets.Select(x => x.ToString("X4")))}}}";
        }
    }
}