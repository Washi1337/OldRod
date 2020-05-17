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
using AsmResolver;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace OldRod.Core.Architecture
{
    public class VMFunctionSignature
    {
        public static VMFunctionSignature FromReader(IBinaryStreamReader reader)
        {
            var result = new VMFunctionSignature
            {
                Flags = reader.ReadByte()
            };
            
            uint count = Utils.ReadCompressedUInt(reader);
            for (var i = 0; i < count; i++)
            {
                result.ParameterTokens.Add(
                    new MetadataToken(Utils.FromCodedToken(Utils.ReadCompressedUInt(reader))));
            }

            result.ReturnToken = new MetadataToken(Utils.FromCodedToken(Utils.ReadCompressedUInt(reader)));

            return result;
        }

        public byte Flags
        {
            get;
            set;
        }

        public IList<MetadataToken> ParameterTokens
        {
            get;
        } = new List<MetadataToken>();

        public MetadataToken ReturnToken
        {
            get;
            set;
        }

        public override string ToString()
        {
            return $"{nameof(Flags)}: {Flags}, {nameof(ParameterTokens)}: {{{string.Join(", ", ParameterTokens)}}}, {nameof(ReturnToken)}: {ReturnToken}";
        }
        
    }
}