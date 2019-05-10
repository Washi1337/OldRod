using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Pipeline.Stages.VMCodeRecovery
{
    public class BruteForceExitKeyResolver : IExitKeyResolver
    {
        public uint? ResolveExitKey(VMConstants constants, VMFunction function)
        {
            return null;
        }
    }
}