using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.Inference
{
    public interface IExitKeyResolver
    {
        uint? ResolveExitKey(VMConstants constants, VMFunction function);
    }
}