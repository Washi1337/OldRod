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
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata;
using AsmResolver.PE.DotNet.Metadata.Tables;
using OldRod.Core.Disassembly;

namespace OldRod.Core.Architecture
{
    public class KoiStream : CustomMetadataStream
    {
        public const int Signature = 0x68736966;
        private const string Tag = "KoiStream";

        public KoiStream(string name, IReadableSegment contents, ILogger logger)
            : base(name, contents)
        {
            var reader = contents.CreateReader();

            logger.Debug(Tag, "Reading koi stream header...");
            uint magic = reader.ReadUInt32();
            
            if (magic != Signature)
                logger.Warning(Tag, $"Koi stream data does not start with a valid signature (Expected 0x{Signature:X4} but read 0x{magic:X4}).");
            
            uint mdCount = reader.ReadUInt32();
            uint strCount = reader.ReadUInt32();
            uint expCount = reader.ReadUInt32();

            logger.Debug(Tag, $"Reading {mdCount} references...");
            for (int i = 0; i < mdCount; i++)
            {
                uint id = Utils.ReadCompressedUInt(ref reader);
                uint token = Utils.FromCodedToken(Utils.ReadCompressedUInt(ref reader));
                References.Add(id, new MetadataToken(token));
            }

            logger.Debug(Tag, $"Reading {strCount} strings...");
            for (int i = 0; i < strCount; i++)
            {
                uint id = Utils.ReadCompressedUInt(ref reader);
                int length = (int) Utils.ReadCompressedUInt(ref reader);

                byte[] buffer = new byte[length * 2];
                reader.ReadBytes(buffer, 0, buffer.Length);
                Strings.Add(id, Encoding.Unicode.GetString(buffer));
            }

            logger.Debug(Tag, $"Reading {expCount} exports...");
            for (int i = 0; i < expCount; i++)
            {
                uint id = Utils.ReadCompressedUInt(ref reader);
                var exportInfo = VMExportInfo.FromReader(ref reader);
                
                // Exports in KoiVM either point to entrypoints of virtualised methods, or just act as a descriptor
                // for methods that are intra linked through instructions like ldftn.
                
                if (exportInfo.IsSignatureOnly)
                    logger.Debug(Tag, $"Export {id} maps to a method signature of an intra-linked method.");
                else
                    logger.Debug(Tag, $"Export {id} maps to function_{exportInfo.EntrypointAddress:X4}.");
                
                Exports.Add(id, exportInfo);
            }
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

        public ModuleDefinition ResolutionContext
        {
            get;
            set;
        }

        public new IReadableSegment Contents => (IReadableSegment) base.Contents;

        public IMetadataMember ResolveReference(ILogger logger, int instructionOffset, uint id, params TableIndex[] expectedMembers)
        {
            if (!References.TryGetValue(id, out var token))
            {
                throw new DisassemblyException($"Detected an invalid reference ID on the stack " +
                                               $"used for resolution at offset IL_{instructionOffset:X4}.");
            }

            if (expectedMembers.Length > 0 && !expectedMembers.Contains(token.Table))
            {
                logger.Warning(Tag,
                    $"Unexpected reference to a {token.Table} member used in one of the arguments of IL_{instructionOffset:X4}.");
            }

            var reference = ResolutionContext.LookupMember(token);
            if (reference is null)
            {
                throw new DisassemblyException(
                    $"Could not resolve the member {token} referenced in one of the arguments of IL_{instructionOffset:X4}.");
            }

            return ToDefinitionInOwnModule(reference) ?? reference;
        }

        private IMetadataMember ToDefinitionInOwnModule(IMetadataMember reference)
        {
            switch (reference)
            {
                case TypeReference type:
                    if (IsDefinedInOwnModule(type))
                        return type.Resolve();
                    break;
                
                case MemberReference member:
                    if (IsDefinedInOwnModule(member.DeclaringType))
                        return member.Resolve();
                    break;
            }

            return null;
        }

        private bool IsDefinedInOwnModule(ITypeDefOrRef type)
        {
            while (type.Scope is TypeReference scope)
                type = scope;

            return type.Scope == ResolutionContext;
        }
    }
}