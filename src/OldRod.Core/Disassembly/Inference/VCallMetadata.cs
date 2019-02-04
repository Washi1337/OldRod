using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.Inference
{
    public abstract class VCallMetadata : InferredMetadata
    {
        public abstract VMCalls VMCall
        {
            get;
        }

        public abstract VMType ReturnType
        {
            get;
        }
    }
}