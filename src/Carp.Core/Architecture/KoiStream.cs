using System.Collections.Generic;
using System.Text;
using AsmResolver;
using AsmResolver.Net;
using AsmResolver.Net.Metadata;

namespace Carp.Core.Architecture
{
    public class KoiStream : MetadataStream
    {
        public static KoiStream FromReader(IBinaryStreamReader reader)
        {
            var result = new KoiStream();
            
            uint magic = reader.ReadUInt32();
            uint mdCount = reader.ReadUInt32();
            uint strCount = reader.ReadUInt32();
            uint expCount = reader.ReadUInt32();

            for (int i = 0; i < mdCount; i++)
            {
                uint id = Utils.ReadCompressedUInt(reader);
                uint token = Utils.FromCodedToken(Utils.ReadCompressedUInt(reader));
                result.References.Add(id, new MetadataToken(token));
            }

            for (int i = 0; i < strCount; i++)
            {
                uint id = Utils.ReadCompressedUInt(reader);
                int length = (int) Utils.ReadCompressedUInt(reader);
                
                result.Strings.Add(id, Encoding.Unicode.GetString(reader.ReadBytes(length*2)));
            }

            for (int i = 0; i < expCount; i++)
            {
                uint id = Utils.ReadCompressedUInt(reader);
                result.Exports.Add(id, VMExportInfo.FromReader(reader));
            }

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

        public override uint GetPhysicalLength()
        {
            throw new System.NotImplementedException();
        }

        public override void Write(WritingContext context)
        {
            throw new System.NotImplementedException();
        }
        
    }
}