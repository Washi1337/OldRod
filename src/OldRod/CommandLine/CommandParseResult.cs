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

        public ICollection<char> Flags
        {
            get;
        } = new HashSet<char>();

        public IDictionary<char, string> Options
        {
            get;
        } = new Dictionary<char, string>();

        public string GetOptionOrDefault(char option, string defaultValue)
        {
            if (!Options.TryGetValue(option, out var value))
                value = defaultValue;
            return value;
        }
    }
}