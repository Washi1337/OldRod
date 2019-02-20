using System.Collections.Generic;
using System.Linq;

namespace OldRod.CommandLine
{
    public class CommandLineSwitch
    {
        internal CommandLineSwitch(IEnumerable<string> identifiers, string description)
        {
            Description = description;
            Identifiers = identifiers.ToList().AsReadOnly();
            CommandLineSwitches.AllSwitches.Add(this);
        }

        internal CommandLineSwitch(IEnumerable<string> identifiers, string description, string defaultValue)
            : this(identifiers, description)
        {
            HasArgument = true;
            DefaultArgument = defaultValue;
        }

        public ICollection<string> Identifiers
        {
            get;
        }

        public string Description
        {
            get;
        }

        public bool HasArgument
        {
            get;
        }

        public string DefaultArgument
        {
            get;
        }

        public override string ToString()
        {
            return "One of " + string.Join(" ", Identifiers);
        }
    }
}