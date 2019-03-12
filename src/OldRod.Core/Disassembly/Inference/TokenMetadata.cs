using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.Inference
{
    public class TokenMetadata : VCallMetadata
    {
        public TokenMetadata(IMetadataMember member)
        {
            Member = member;
        }
        
        public override VMCalls VMCall => VMCalls.TOKEN;

        public IMetadataMember Member
        {
            get;
        }

        public override VMType ReturnType => VMType.Pointer;
    }
}