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
using AsmResolver;
using AsmResolver.IO;
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Metadata;
using OldRod.Core;
using OldRod.Core.Architecture;

namespace OldRod.Pipeline
{
    public class KoiVmAwareStreamReader : IMetadataStreamReader
    {
        private readonly IMetadataStreamReader _reader;

        public KoiVmAwareStreamReader(ILogger logger)
            : this("#Koi", logger)
        {
        }

        public KoiVmAwareStreamReader(string koiStreamName, ILogger logger)
        {
            KoiStreamName = koiStreamName ?? throw new ArgumentNullException(nameof(koiStreamName));
            Logger = logger;
            _reader = new DefaultMetadataStreamReader();
        }
        
        public string KoiStreamName
        {
            get;
        }

        public ILogger Logger
        {
            get;
        }

        public IMetadataStream ReadStream(PEReaderContext context, MetadataStreamHeader header, ref BinaryStreamReader reader)
        {
            return header.Name == KoiStreamName
                ? new KoiStream(KoiStreamName, new DataSegment(reader.ReadToEnd()), Logger)
                : _reader.ReadStream(context, header, ref reader);
        }
    }
}