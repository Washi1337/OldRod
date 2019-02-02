namespace OldRod.Core.Disassembly.Inference
{
    public class InferredMetadata
    {
        public int InferredPushCount
        {
            get;
            set;
        }

        public int InferredPopCount
        {
            get;
            set;
        }

        public override string ToString()
        {
            return "Pop: " + InferredPopCount + ", Push: " + InferredPushCount;
        }
    }
}