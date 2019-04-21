namespace OldRod.Core.Disassembly.Inference
{
    public interface IVMFunctionFactory
    {
        VMFunction CreateFunction(uint entryAddress, uint entryKey);
    }
}