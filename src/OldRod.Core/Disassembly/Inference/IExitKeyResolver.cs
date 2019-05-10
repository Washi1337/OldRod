using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.Inference
{
    public interface IExitKeyResolver
    {
        string Name
        {
            get;
        }
        
        uint? ResolveExitKey(ILogger logger, KoiStream koiStream, VMConstants constants, VMFunction function);
    }
}