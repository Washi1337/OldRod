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