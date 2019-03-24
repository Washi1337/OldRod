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
using System.Text;
using AsmResolver;
using AsmResolver.Net;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using OldRod.Core.Disassembly;

namespace OldRod.Core.Architecture
{
    public class KoiStream : CustomMetadataStream
    {
        private const string Tag = "KoiStream";
        
        public static KoiStream FromReadingContext(ReadingContext context, ILogger logger)
        {
            var reader = context.Reader;
            var result = new KoiStream {StartOffset = reader.Position};

            logger.Debug(Tag, "Reading koi stream header...");
            uint magic = reader.ReadUInt32();
            uint mdCount = reader.ReadUInt32();
            uint strCount = reader.ReadUInt32();
            uint expCount = reader.ReadUInt32();

            logger.Debug(Tag, $"Reading {mdCount} references...");
            for (int i = 0; i < mdCount; i++)
            {
                uint id = Utils.ReadCompressedUInt(reader);
                uint token = Utils.FromCodedToken(Utils.ReadCompressedUInt(reader));
                result.References.Add(id, new MetadataToken(token));
            }

            logger.Debug(Tag, $"Reading {strCount} strings...");
            for (int i = 0; i < strCount; i++)
            {
                uint id = Utils.ReadCompressedUInt(reader);
                int length = (int) Utils.ReadCompressedUInt(reader);
                
                result.Strings.Add(id, Encoding.Unicode.GetString(reader.ReadBytes(length*2)));
            }

            logger.Debug(Tag, $"Reading {expCount} exports...");
            for (int i = 0; i < expCount; i++)
            {
                uint id = Utils.ReadCompressedUInt(reader);
                var exportInfo = VMExportInfo.FromReader(reader);
                
                // Exports in KoiVM either point to entrypoints of virtualised methods, or just act as a descriptor
                // for methods that are intra linked through instructions like ldftn.
                
                if (exportInfo.IsSignatureOnly)
                    logger.Debug(Tag, $"Export {id} maps to a method signature of an intra-linked method.");
                else
                    logger.Debug(Tag, $"Export {id} maps to function_{exportInfo.CodeOffset:X4}.");
                
                result.Exports.Add(id, exportInfo);
            }

            reader.Position = reader.StartPosition;
            result.Data = reader.ReadBytes((int) reader.Length);

            return result;
        }

        public IDictionary<uint, MetadataToken> References
        {
            get;
        } = new Dictionary<uint, MetadataToken>();

        public IDictionary<uint, string> Strings
        {
            get;
        } = new Dictionary<uint, string>();

        public IDictionary<uint, VMExportInfo> Exports
        {
            get;
        } = new Dictionary<uint, VMExportInfo>();

        public IMetadataMember ResolveReference(ILogger logger, int instructionOffset, uint id, params MetadataTokenType[] expectedMembers)
        {
            if (!References.TryGetValue(id, out var token))
            {
                throw new DisassemblyException($"Detected an invalid reference ID on the stack " +
                                               $"used for resolution at offset IL_{instructionOffset:X4}.");
            }

            if (expectedMembers.Length > 0 && !expectedMembers.Contains(token.TokenType))
            {
                logger.Warning(Tag,
                    $"Unexpected reference to a {token.TokenType} member used in one of the arguments of IL_{instructionOffset:X4}.");
            }

            try
            {
                return StreamHeader.MetadataHeader.Image.ResolveMember(token);
            }
            catch (MemberResolutionException ex)
            {
                throw new DisassemblyException(
                    $"Could not resolve the member {token} referenced in one of the arguments of IL_{instructionOffset:X4}.",
                    ex);
            }
        }
        
    }
}