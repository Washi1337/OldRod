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
using System.IO;

namespace OldRod.Pipeline
{
    public class OutputOptions
    {
        public string RootDirectory
        {
            get;
            set;
        }

        public string DumpsDirectory => Path.Combine(RootDirectory, "Dumps");

        public string ILDumpsDirectory => Path.Combine(DumpsDirectory, "IL");
        
        public string ILAstDumpsDirectory => Path.Combine(DumpsDirectory, "IL-AST");
        
        public string CilAstDumpsDirectory => Path.Combine(DumpsDirectory, "CIL-AST");
        
        public string CilDumpsDirectory => Path.Combine(DumpsDirectory, "CIL");
        
        public bool DumpControlFlowGraphs
        {
            get;
            set;
        }
        
        public bool DumpAllControlFlowGraphs
        {
            get;
            set;
        }

        public bool DumpDisassembledIL
        {
            get;
            set;
        }

        public bool DumpRecompiledCil
        {
            get;
            set;
        }

        private IEnumerable<string> GetNecessaryDirectories()
        {
            var result = new List<string> {RootDirectory};

            if (DumpDisassembledIL || DumpControlFlowGraphs || DumpRecompiledCil)
                result.Add(ILDumpsDirectory);
            
            if (DumpControlFlowGraphs)
            {
                result.Add(ILAstDumpsDirectory);
                result.Add(CilAstDumpsDirectory);
            }

            if (DumpRecompiledCil)
                result.Add(CilDumpsDirectory);
            
            return result;
        }

        public void EnsureDirectoriesExist()
        {
            foreach (var directory in GetNecessaryDirectories())
            {
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }
        }
        
    }
}