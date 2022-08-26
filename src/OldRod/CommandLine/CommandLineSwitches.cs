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
        }, "Prevent the \"Press any key to continue...\" from appearing on exit.");
        
        public static readonly CommandLineSwitch VerboseOutput = new CommandLineSwitch(new[]
        {
            "v", "-verbose"
        }, "Enable verbose output. Useful for debugging purposes.");
        
        public static readonly CommandLineSwitch VeryVerboseOutput = new CommandLineSwitch(new[]
        {
            "vv", "-very-verbose"
        }, "Enable very verbose output. Useful for debugging purposes, but can get big very quickly.");
        
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
        
        public static readonly CommandLineSwitch DumpCIL = new CommandLineSwitch(new[]
        {
            "-dump-cil"
        }, "Dump recompiled CIL for each function defined in the #Koi stream.");
        
        public static readonly CommandLineSwitch RenameConstants = new CommandLineSwitch(new[]
        {
            "-rename-symbols" 
        }, "Rename all VM configuration fields, opcode interfaces and classes and the like in the runtime assembly.");
        
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
        
        public static readonly CommandLineSwitch OverrideVMContextType = new CommandLineSwitch(new[]
        {
            "-context-type"
        }, "Override metadata token for VMContext type (instead of auto detection).", null);

        public static readonly CommandLineSwitch KoiStreamName = new CommandLineSwitch(new[]
        {
            "kn", "-koi-stream-name"
        }, "Override name of KoiVM metadata stream (instead of #Koi).", "#Koi");
        
        public static readonly CommandLineSwitch KoiStreamData = new CommandLineSwitch(new[]
        {
            "kd", "-koi-stream-data"
        }, "Override data of KoiVM metadata stream by the specified file (instead of stream embedded into the target file).", null);
        
        public static readonly CommandLineSwitch IgnoreExports = new CommandLineSwitch(new[]
        {
            "-ignore-export"
        }, "Prevent all specified exports from being devirtualised (comma-separated string of export IDs).", null);

        public static readonly CommandLineSwitch OnlyExports = new CommandLineSwitch(new[]
        {
            "-only-export"
        }, "Only devirtualise all specified exports (comma-separated string of export IDs).", null);

        public static readonly CommandLineSwitch IgnoreMethods = new CommandLineSwitch(new[]
        {
            "-ignore-method"
        }, "Prevent all specified all specified methods from being devirtualised (comma-separated string of RIDs).", null);

        public static readonly CommandLineSwitch OnlyMethods = new CommandLineSwitch(new[]
        {
            "-only-method"
        }, "Only devirtualise all specified methods (comma-separated string of RIDs).", null);

        public static readonly CommandLineSwitch NoExportMapping = new CommandLineSwitch(new[]
        {
            "-no-export-mapping"
        }, "Prevent all exports from being mapped to physical methods and only create new physical methods in <Module>."); 
            
        public static readonly CommandLineSwitch IgnoreInvalidMD = new CommandLineSwitch(new[]
        {
            "-ignore-invalid-md"
        }, "Ignores all invalid metadata.");
        
        public static readonly CommandLineSwitch OutputLogFile = new CommandLineSwitch(new[]
        {
            "l", "-log-file"
        }, "Create a log file in the output directory of the process.");
        
        public static readonly CommandLineSwitch RuntimeLibFileName = new CommandLineSwitch(new[]
        {
            "rt", "-runtime-path"
        }, "Force runtime library file path (instead of auto detection). This can be a relative or an absolute path.", null);
        
        public static readonly CommandLineSwitch ForceEmbeddedRuntimeLib = new CommandLineSwitch(new[]
        {
            "-runtime-embedded"
        }, "Force runtime library to be embedded in the target assembly (instead of auto detection).");
        
        public static readonly CommandLineSwitch ConfigurationFile = new CommandLineSwitch(new[]
        {
            "-config"
        }, "Use opcode configuration from the provided JSON file.", null);
        
        public static readonly CommandLineSwitch MaxSimplificationPasses = new CommandLineSwitch(new[]
        {
            "-max-opt-passes"
        }, "Specify a maximum amount of iterations the logic simplifier can use for optimizing expressions.", null);
        
        public static readonly CommandLineSwitch RunMethod1Signature = new CommandLineSwitch(new[]
        {
            "r1", "-run-sig-1"
        }, "Specify a comma-separated list of parameter type names of the first run method.", null);
        
        public static readonly CommandLineSwitch RunMethod2Signature = new CommandLineSwitch(new[]
        {
            "r2", "-run-sig-2"
        }, "Specify a comma-separated list of parameter type names of the second run method.", null);
        
        public static readonly CommandLineSwitch SalvageData = new CommandLineSwitch(new[]
        {
            "-salvage"
        }, "Salvage as much data as possible when an error occurs.");
        
        public static readonly CommandLineSwitch EnableTroublenoobing = new CommandLineSwitch(new[]
        {
            "-dont-crash", "-no-errors", "-no-output-corruption"
        }, "Enable additional troubleshooting settings.");
        
    }
}