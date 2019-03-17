using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.Inference
{
    public class TokenMetadata : VCallMetadata
    {
        public TokenMetadata(IMetadataMember member)
            : base(VMCalls.TOKEN, VMType.Pointer)
        {
            Member = member;
        }
       
        public IMetadataMember Member
        {
            get;
        }
    }
}