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
        }, "Dump control flow graphs after each AST optimisation step.");
        
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