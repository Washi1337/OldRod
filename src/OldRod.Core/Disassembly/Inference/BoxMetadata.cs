using AsmResolver.Net.Cts;

namespace OldRod.Core.Disassembly.Inference
{
    public class BoxMetadata : InferredMetadata
    {
        public BoxMetadata(ITypeDefOrRef boxedType, object value)
        {
            BoxedType = boxedType;
            Value = value;
        }
        
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