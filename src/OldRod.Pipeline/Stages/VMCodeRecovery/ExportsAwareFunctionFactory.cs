using System;
using System.Linq;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Pipeline.Stages.VMCodeRecovery
{
    public class ExportsAwareFunctionFactory : IVMFunctionFactory
    {
        private readonly DevirtualisationContext _context;

        public ExportsAwareFunctionFactory(DevirtualisationContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
     
        public VMFunction CreateFunction(uint entryAddress, uint entryKey)
        {
            return _context.VirtualisedMethods
                       .FirstOrDefault(m => m.Function.EntrypointAddress == entryAddress)
                       ?.Function
                   ?? new VMFunction(entryAddress, entryKey);
        }
    }
}