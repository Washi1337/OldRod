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

namespace OldRod.Core
{
    public class FilteredLogger : ILogger
    {
        private readonly ILogger _logger;

        public FilteredLogger(ILogger logger)
        {
            _logger = logger;
        }

        public bool IncludeDebug2
        {
            get;
            set;
        } = false;

        public bool IncludeDebug
        {
            get;
            set;
        } = true;

        public bool IncludeLog
        {
            get;
            set;
        } = true;

        public bool IncludeWarning
        {
            get;
            set;
        } = true;

        public bool IncludeError
        {
            get;
            set;
        } = true;

        public void Debug2(string tag, string message)
        {
            if (IncludeDebug2)
                _logger.Debug2(tag, message);
        }

        public void Debug(string tag, string message)
        {
            if (IncludeDebug)
                _logger.Debug(tag, message);
        }

        public void Log(string tag, string message)
        {
            if (IncludeLog)
                _logger.Log(tag, message);
        }

        public void Warning(string tag, string message)
        {
            if (IncludeWarning)
                _logger.Warning(tag, message);
        }

        public void Error(string tag, string message)
        {
            if (IncludeError)
                _logger.Error(tag, message);
        }
    }
}