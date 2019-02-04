using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.Inference
{
    public class BoxMetadata : VCallMetadata
    {
        public BoxMetadata(ITypeDefOrRef boxedType, object value)
        {
            BoxedType = boxedType;
            Value = value;
        }
        
        public override VMCalls VMCall => VMCalls.BOX;

        public override VMType ReturnType => VMType.Object;
                
        public ITypeDefOrRef BoxedType
        {
            get;
        }

        public object Value
        {
            get;
        }

        public override string ToString()
        {
            return $"BOX {BoxedType} ({(Value ?? "?")})";
        }

    }
}