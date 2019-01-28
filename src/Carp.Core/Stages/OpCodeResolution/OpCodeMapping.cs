using System.Collections.Generic;
using AsmResolver.Net.Cts;

namespace Carp.Core.Stages.OpCodeResolution
{
    public class OpCodeMapping
    {
        public OpCodeMapping(IDictionary<byte, OpCodeInfo> opCodes, IDictionary<byte, TypeDefinition> vCalls)
        {
            OpCodes = opCodes;
            VCalls = vCalls;
        }
        
        public IDictionary<byte, OpCodeInfo> OpCodes { get; }

        public IDictionary<byte, TypeDefinition> VCalls { get; }
    }
}