using System.Collections.Generic;

namespace OldRod.CommandLine
{
    public class CommandLineParser
    {
        private IDictionary<string, CommandLineSwitch> _flags = new Dictionary<string, CommandLineSwitch>();
        private IDictionary<string, CommandLineSwitch> _options = new Dictionary<string, CommandLineSwitch>();

        public void AddSwitch(CommandLineSwitch @switch)
        {
            IDictionary<string, CommandLineSwitch> collection = @switch.HasArgument
                ? _options
                : _flags;

            foreach (var identifier in @switch.Identifiers)
                collection[identifier] = @switch;
        }
        
        public CommandParseResult Parse(string[] args)
        {
            var result = new CommandParseResult();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i][0] == '-')
                {
                    string word = args[i].Substring(1);

                    if (_flags.TryGetValue(word, out var flag))
                    {
                        result.Flags.Add(flag);
                    }
                    else if (_options.TryGetValue(word, out var option))
                    {
                        result.Options[option] = args[i + 1];
                        i++;
                    }
                    else
                    {
                        throw new CommandLineParseException($"Unknown flag or option -{word}.");
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

            return result;
        }        
    }
}