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