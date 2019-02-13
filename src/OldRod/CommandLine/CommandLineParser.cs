using System.Collections.Generic;

namespace OldRod.CommandLine
{
    public class CommandLineParser
    {
        public ICollection<char> Flags
        {
            get;
        } = new HashSet<char>();
        
        public ICollection<char> Options
        {
            get;
        } = new HashSet<char>();
        
        public CommandParseResult Parse(string[] args)
        {
            var result = new CommandParseResult();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i][0] == '-')
                {
                    for (int j = 1; j < args[i].Length; j++)
                    {
                        char c = args[i][j];
                        if (Flags.Contains(c))
                        {
                            result.Flags.Add(c);
                        }
                        else if (Options.Contains(c))
                        {
                            result.Options[c] = args[i + 1].Replace("\"", "");
                            i++;
                            break;
                        }
                        else
                        {
                            throw new CommandLineParseException($"Unknown flag or option {c}.");
                        }
                    }
                }
                else if (result.FilePath == null)
                {
                    result.FilePath = args[i].Replace("\"", "");
                }
                else
                {
                    throw new CommandLineParseException("Too many input files specified.");
                }
            }

            if (result.FilePath == null)
                throw new CommandLineParseException("No input file path specified.");
            return result;
        }        
    }
}