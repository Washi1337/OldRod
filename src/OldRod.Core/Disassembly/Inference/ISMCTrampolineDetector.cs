using OldRod.Core.Disassembly.DataFlow;

namespace OldRod.Core.Disassembly.Inference 
{
    public interface ISMCTrampolineDetector 
    {
        bool IsSMCTrampoline(ProgramState currentState, out byte smcKey, out ulong smcTrampolineEnd);
    }
}