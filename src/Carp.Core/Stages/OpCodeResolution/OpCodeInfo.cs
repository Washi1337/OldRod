using AsmResolver.Net.Cts;
using Carp.Core.Architecture;

namespace Carp.Core.Stages.OpCodeResolution
{
    public struct OpCodeInfo
    {
        public OpCodeInfo(TypeDefinition opCodeType, ILCode opCode)
        {
            OpCodeType = opCodeType;
            OpCode = opCode;
        }

        public TypeDefinition OpCodeType { get; }

        public ILCode OpCode { get; }

        public override string ToString()
        {
            return $"{OpCode} ({OpCodeType.MetadataToken})";
        }
    }
}