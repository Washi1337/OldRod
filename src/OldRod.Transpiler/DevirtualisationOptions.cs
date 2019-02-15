using System;
using System.IO;

namespace OldRod.Transpiler
{
    public class DevirtualisationOptions
    {
        public DevirtualisationOptions(string inputFile)
            : this(inputFile, Path.Combine(Path.GetDirectoryName(inputFile), "Devirtualised"))
        {
        }

        public DevirtualisationOptions(string inputFile, string outputDirectory)
        {
            InputFile = inputFile ?? throw new ArgumentNullException(nameof(inputFile));
            OutputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
        }
        
        public string InputFile
        {
            get;
        }

        public string OutputDirectory
        {
            get;
        }
        
        public bool DumpControlFlowGraphs
        {
            get;
            set;
        }

        public bool DumpDisassembledIL
        {
            get;
            set;
        }
    }
}