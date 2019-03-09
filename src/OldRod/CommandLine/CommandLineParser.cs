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