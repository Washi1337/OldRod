using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cts;

namespace OldRod.Core.Architecture
{
    public class VMConstants
    {
        public IDictionary<FieldDefinition, byte> ConstantFields
        {
            get;
        } = new Dictionary<FieldDefinition, byte>();
        
        public IDictionary<byte, ILCode> OpCodes
        {
            get;
        } = new Dictionary<byte, ILCode>();
        
        public IDictionary<byte, VMFlags> Flags
        {
            get;
        } = new Dictionary<byte, VMFlags>();
        
        public IDictionary<byte, VMRegisters> Registers
        {
            get;
        } = new Dictionary<byte, VMRegisters>();
        
        public IDictionary<byte, VMCalls> VMCalls
        {
            get;
        } = new Dictionary<byte, VMCalls>();

        public byte HelperInit
        {
            get;
            set;
        }

        public IDictionary<byte, VMECallOpCode> ECallOpCodes
        {
            get;
        } = new Dictionary<byte, VMECallOpCode>();

        public byte FlagInstance
        {
            get;
            set;
        }

        public byte GetFlagMask(VMFlags flags)
        {
            byte result = 0;

            foreach (var entry in Flags)
            {
                if (flags.HasFlag(entry.Value))
                    result |= entry.Key;
            }

            return result;
        }
    }
}