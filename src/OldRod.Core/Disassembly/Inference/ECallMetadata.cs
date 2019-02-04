using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.Inference
{
    public class ECallMetadata : VCallMetadata
    {
        public ECallMetadata(IMethodDefOrRef method, VMECallOpCode opCode)
        {
            Method = method;
            OpCode = opCode;
        }
        
        public override VMCalls VMCall => VMCalls.ECALL;

        public override VMType ReturnType => ((MethodSignature) Method.Signature).ReturnType.ToVMType();
        
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