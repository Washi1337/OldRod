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

using System;
using System.Collections.Generic;
using System.IO;
using AsmResolver.Net.Metadata;

namespace OldRod.Pipeline
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
            OutputOptions.RootDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
        }
        
        public string InputFile
        {
            get;
        }

        public string RuntimeFile
        {
            get;
            set;
        }

        public bool RuntimeIsEmbedded => RuntimeFile == InputFile;

        public bool AutoDetectRuntimeFile => RuntimeFile == null;
        
        public OutputOptions OutputOptions
        {
            get;
        } = new OutputOptions();

        public bool OverrideVMConstantsToken
        {
            get;
            set;
        }

        public MetadataToken VMConstantsToken
        {
            get;
            set;
        }

        public bool OverrideVMEntryToken
        {
            get;
            set;
        }

        public MetadataToken VMEntryToken
        {
            get;
            set;
        }
        public string KoiStreamName
        {
            get;
            set;
        } = "#Koi";

        public ICollection<uint> IgnoredExports
        {
            get;
        } = new HashSet<uint>();

        public bool RenameConstants
        {
            get;
            set;
        }
            
    }
}