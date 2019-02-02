using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.Inference
{
    public class ECallMetadata : InferredMetadata
    {
        public ECallMetadata(IMethodDefOrRef method, VMECallOpCode opCode)
        {
            Method = method;
            OpCode = opCode;
        }
        
        public IMethodDefOrRef Method
        {
            get;
        }

        public VMECallOpCode OpCode
        {
            get;
        }

        public override string ToString()
        {
            return $"{OpCode} {Method}";
        }
    }
}