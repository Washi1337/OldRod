using AsmResolver.DotNet;

namespace OldRod.Core.Disassembly.Annotations
{
    public interface IMemberProvider
    {
        IMemberDescriptor Member
        {
            get;
        }
        
        bool RequiresSpecialAccess
        {
            get;
        }
    }
}