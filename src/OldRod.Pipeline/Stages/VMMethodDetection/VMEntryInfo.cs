using AsmResolver.Net.Cts;

namespace OldRod.Pipeline.Stages.VMEntryDetection
{
    public class VMEntryInfo
    {
        public TypeDefinition VMEntryType
        {
            get;
            set;
        }
        
        public MethodDefinition RunMethod1
        {
            get;
            set;
        }
        
        public MethodDefinition RunMethod2
        {
            get;
            set;
        }
    }
}