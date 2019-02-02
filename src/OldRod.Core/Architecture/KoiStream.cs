using System.Collections.Generic;
using System.Text;
using AsmResolver;
using AsmResolver.Net;
using AsmResolver.Net.Metadata;

namespace OldRod.Core.Architecture
{
    public class KoiStream : CustomMetadataStream
    {
        public static KoiStream FromBytes(byte[] data)
        {
            var reader = new MemoryStreamReader(data);
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

            result.Data = data;

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

        public int HeaderSize
        {
            get;
            set;
        }
        
    }
}