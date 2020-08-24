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
using System.IO;
using AsmResolver.PE.DotNet.Metadata.Tables;
using OldRod.Core.Architecture;

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
        
        /// <summary>
        /// Gets the path to the input file.
        /// </summary>
        public string InputFile
        {
            get;
        }

        /// <summary>
        /// Gets or sets the path to the assembly containing the KoiVM runtime. 
        /// </summary>
        /// <remarks>
        /// If this value is <c>null</c>, the devirtualizer will attempt to auto-detect the path to the runtime assembly.
        /// If this value is the same as the input file, the devirtualizer will assume the runtime is embedded into the
        /// input file.
        /// </remarks>
        public string RuntimeFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether the runtime is embedded into the target assembly or not.
        /// </summary>
        public bool RuntimeIsEmbedded => RuntimeFile == InputFile;

        /// <summary>
        /// Gets a value indicating whether the location of the runtime should be auto detected or not.
        /// </summary>
        public bool AutoDetectRuntimeFile => RuntimeFile == null;
        
        /// <summary>
        /// Gets options regarding output generation.
        /// </summary>
        public OutputOptions OutputOptions
        {
            get;
        } = new OutputOptions();

        /// <summary>
        /// Gets a value indicating whether a metadata token of the type containing the VM constants was provided. 
        /// </summary>
        public bool OverrideVMConstantsToken => VMConstantsToken != null;

        /// <summary>
        /// Gets or sets a value indicating the metadata token of the type containing the VM constants. 
        /// </summary>
        /// <remarks>
        /// If this value is <c>null</c>, then the devirtualizer will attempt to auto-detect the constants type.
        /// This value is ignored if <see cref="OverrideConstants"/> is <c>True</c>.
        /// </remarks>
        public MetadataToken? VMConstantsToken
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether a set of VM constants is assigned to use for devirtualizing the input file.
        /// </summary>
        public bool OverrideConstants => Constants != null;
        
        /// <summary>
        /// Gets or sets the VM constants that should be used by the devirtualizer. 
        /// </summary>
        /// <remarks>
        /// If this value is <c>null</c>, the devirtualizer will attempt to auto-detect the constants for the provided
        /// input file.
        /// </remarks>
        public VMConstants Constants
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether a metadata token to the VMEntry type was provided.
        /// </summary>
        public bool OverrideVMEntryToken => VMEntryToken != null;

        /// <summary>
        /// Gets or sets the metadata token to the VMEntry type.
        /// </summary>
        /// <remarks>
        /// If this value is <c>null</c>, the devirtualizer will attempt to auto-detect the VMEntry class for the provided
        /// input file.
        /// </remarks>
        public MetadataToken? VMEntryToken
        {
            get;
            set;
        }
        
        /// <summary>
        /// Gets or sets the name of the metadata stream containing the VM data. 
        /// </summary>
        public string KoiStreamName
        {
            get;
            set;
        } = "#Koi";

        /// <summary>
        /// Gets a value indicating whether an external file was provided containing the VM data. 
        /// </summary>
        public bool OverrideKoiStreamData => KoiStreamDataFile != null;

        /// <summary>
        /// Gets or sets the path to a file containing the VM data.
        /// </summary>
        /// <remarks>
        /// If this value is <c>null</c>, the devirtualizer will assume the VM data is in the metadata stream with the name
        /// specified in <see cref="KoiStreamName"/>. 
        /// </remarks>
        public string KoiStreamDataFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a selection of exports to devirtualize.
        /// </summary>
        public IdSelection SelectedExports
        {
            get;
            set;
        } = IdSelection.All;

        /// <summary>
        /// Gets or sets a selection of methods to devirtualize.
        /// </summary>
        public IdSelection SelectedMethods
        {
            get;
            set;
        } = IdSelection.All;

        /// <summary>
        /// Gets or sets a value indicating whether exports defined by the VM data should be mapped to physical methods
        /// in the input file.
        /// </summary>
        public bool NoExportMapping
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the devirtualizer should ignore all invalid method bodies that might
        /// be present in the target assembly.
        /// </summary>
        public bool IgnoreInvalidMethodBodies
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the devirtualizer should rename runtime symbols.
        /// </summary>
        public bool RenameSymbols
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating the devirtualizer should work in salvage mode. That is, ignore as many errors
        /// as possible if they occur. 
        /// </summary>
        /// <remarks>
        /// This is a dangerous option to enable, and can produce invalid binaries or undefined behaviour in the
        /// devirtualizer.
        /// </remarks>
        public bool EnableSalvageMode
        {
            get;
            set;
        }
        
    }
}