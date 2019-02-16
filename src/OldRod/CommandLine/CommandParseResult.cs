using System.Collections.Generic;

namespace OldRod.CommandLine
{
    public class CommandParseResult
    {
        public string FilePath
        {
            get;
            set;
        }

        public ICollection<CommandLineSwitch> Flags
        {
            get;
        } = new List<CommandLineSwitch>();

        public IDictionary<CommandLineSwitch, string> Options
        {
            get;
        } = new Dictionary<CommandLineSwitch, string>();

        public string GetOptionOrDefault(CommandLineSwitch option)
        {
            if (!Options.TryGetValue(option, out string value))
                value = option.DefaultArgument;
            return value;
        }
        
        public string GetOptionOrDefault(CommandLineSwitch option, string defaultValue)
        {
            if (!Options.TryGetValue(option, out string value))
                value = defaultValue;
            return value;
        }
    }
}