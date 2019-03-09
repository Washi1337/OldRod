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

        public IDictionary<string, IList<ILAstNode>> Captures
        {
            get;
        } = new Dictionary<string, IList<ILAstNode>>();

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

        public override string ToString()
        {
            return $"{nameof(Success)}: {Success}";
        }
    }
}