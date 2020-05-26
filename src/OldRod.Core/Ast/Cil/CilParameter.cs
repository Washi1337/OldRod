using AsmResolver.DotNet.Signatures.Types;

namespace OldRod.Core.Ast.Cil
{
    public class CilParameter : CilVariable
    {
        public CilParameter(string name, TypeSignature variableType, int parameterIndex, bool hasFixedType)
            : base(name, variableType)
        {
            ParameterIndex = parameterIndex;
            HasFixedType = hasFixedType;
        }

        public int ParameterIndex
        {
            get;
        }

        public bool HasFixedType
        {
            get;
        }
    }
}