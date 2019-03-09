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