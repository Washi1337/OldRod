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

namespace OldRod.CommandLine
{
    public static class CommandLineSwitches
    {
        public static readonly ICollection<CommandLineSwitch> AllSwitches = new List<CommandLineSwitch>();
        
        public static readonly CommandLineSwitch Help = new CommandLineSwitch(new[]
        {
            "h", "-help"
        }, "Shows help.");
        
        public static readonly CommandLineSwitch NoPause = new CommandLineSwitch(new[]
        {
            "-no-pause"
        }, "Prevents the \"Press any key to continue...\" from appearing on exit.");
        
        public static readonly CommandLineSwitch VerboseOutput = new CommandLineSwitch(new[]
        {
            "v", "-verbose"
        }, "Enable verbose output. Useful for debugging purposes.");
        
        public static readonly CommandLineSwitch DumpIL = new CommandLineSwitch(new[]
        {
            "-dump-il"
        }, "Dump disassembled KoiVM IL code of each function defined in the #Koi stream.");
        
        public static readonly CommandLineSwitch DumpCfg = new CommandLineSwitch(new[]
        {
            "-dump-cfg"
        }, "Dump reconstructed control flow graphs of each function defined in the #Koi stream.");
        
        public static readonly CommandLineSwitch DumpAllCfg = new CommandLineSwitch(new[]
        {
            "-dump-cfg-all"
        }, "Dump control flow graphs after each AST optimisation step (Useful for debugging).");
        
        public static readonly CommandLineSwitch RenameConstants = new CommandLineSwitch(new[]
        {
            "-rename-constants" 
        }, "Renames all VM configuration fields, opcode interfaces and classes and the like in the runtime assembly.");
        
        public static readonly CommandLineSwitch OutputDirectory = new CommandLineSwitch(new[]
        {
            "o", "-output-directory"
        }, "Set output directory of the devirtualiser.", null);
        
        public static readonly CommandLineSwitch OverrideVMEntry = new CommandLineSwitch(new[]
        {
            "-entry-type"
        }, "Override metadata token for VMEntry type (instead of auto detection).", null);

        public static readonly CommandLineSwitch OverrideVMConstants = new CommandLineSwitch(new[]
        {
            "-constants-type"
        }, "Override metadata token for VMConstants type (instead of auto detection).", null);

        public static readonly CommandLineSwitch KoiStreamName = new CommandLineSwitch(new[]
        {
            "k", "-koi-stream"
        }, "Override name of KoiVM metadata stream (instead of #Koi).", "#Koi");
        
        public static readonly CommandLineSwitch IgnoreExport = new CommandLineSwitch(new[]
        {
            "-ignore-export"
        }, "Prevents all exports that are provided in a comma-separated string from being devirtualised.", null);
        
    }
}