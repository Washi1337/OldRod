using System.Collections.Generic;
using AsmResolver;
using AsmResolver.Net.Metadata;

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
            return $"{nameof(Flags)}: {Flags}, {nameof(ParameterTokens)}: {ParameterTokens}, {nameof(ReturnToken)}: {ReturnToken}";
        }
    }
}