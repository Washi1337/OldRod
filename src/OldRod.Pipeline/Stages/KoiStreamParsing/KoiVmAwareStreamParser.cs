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
using AsmResolver.Net;
using OldRod.Core;
using OldRod.Core.Architecture;

namespace OldRod.Pipeline.Stages.KoiStreamParsing
{
    public class KoiVmAwareStreamParser : 
    {
        private readonly DefaultMetadataStreamParser _parser = new DefaultMetadataStreamParser();

        public KoiVmAwareStreamParser(ILogger logger)
            : this("#Koi", logger)
        {
        }

        public KoiVmAwareStreamParser(string koiStreamName, ILogger logger)
        {
            KoiStreamName = koiStreamName ?? throw new ArgumentNullException(nameof(koiStreamName));
            Logger = logger;
        }
        
        public string KoiStreamName
        {
            get;
        }

        public byte[] ReplacementData
        {
            get; 
            set;
        }
        
        public ILogger Logger
        {
            get;
        }

        public MetadataStream ReadStream(string streamName, ReadingContext context)
        {
            if (streamName != KoiStreamName)
                return _parser.ReadStream(streamName, context);

            if (ReplacementData != null)
            {
                context = new ReadingContext()
                {
                    Reader = new MemoryStreamReader(ReplacementData)
                };
            }
            
            return KoiStream.FromReadingContext(context, Logger);
        }
    }
}