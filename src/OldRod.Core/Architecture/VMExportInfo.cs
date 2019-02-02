using AsmResolver;

namespace OldRod.Core.Architecture
{
    public class VMExportInfo
    {
        public static VMExportInfo FromReader(IBinaryStreamReader reader)
        {
            uint offset = reader.ReadUInt32();
            uint entryKey = offset != 0 ? reader.ReadUInt32() : 0;

            return new VMExportInfo
            {
                CodeOffset = offset,
                EntryKey = entryKey,
                Signature = VMFunctionSignature.FromReader(reader)
            };
        }
        
        public uint CodeOffset
        {
            get;
            set;
        }

        public uint EntryKey
        {
            get;
            set;
        }

        public VMFunctionSignature Signature
        {
            get;
            set;
        }

        public override string ToString()
        {
            return $"{nameof(CodeOffset)}: {CodeOffset:X8}, {nameof(EntryKey)}: {EntryKey:X8}, {nameof(Signature)}: {Signature}";
        }
    }
}