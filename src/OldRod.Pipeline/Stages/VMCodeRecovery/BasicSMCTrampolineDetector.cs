using OldRod.Core.Architecture;
using OldRod.Core.Disassembly;
using OldRod.Core.Disassembly.DataFlow;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Pipeline.Stages.VMCodeRecovery 
{
    public sealed class BasicSMCTrampolineDetector : ISMCTrampolineDetector 
    {
        private readonly InstructionDecoder _instructionDecoder;
        
        public BasicSMCTrampolineDetector(VMConstants constants, KoiStream stream) 
        {
            _instructionDecoder = new InstructionDecoder(constants, stream.CreateReader());
        }
        
        public bool IsSMCTrampoline(ProgramState currentState, out byte smcKey, out ulong smcTrampolineEnd) 
        {
            smcKey = 0;
            smcTrampolineEnd = 0;
            
            // The first instruction of the SMC trampoline block is
            // preceeded by a single byte key used to decrypt it.
            
            // Make sure we can actually read the single byte key byte.
            if (currentState.IP < 1)
                return false;

            _instructionDecoder.ReaderOffset = (uint)(currentState.IP - 1);
            
            // We can't use the regular ReadByte() method as the key byte is not encrypted.
            smcKey = _instructionDecoder.ReadNonEncryptedByte();

            // If the previous byte is a 0, no additional processing is necessary as A xor 0 = A.
            // This check is also present in the code injected as part of the SMC decryption routine.
            if (smcKey == 0)
                return false;
            
            // Set correct decoder state. Setting SMCTrampolineKey causes the
            // deocder to decrypt the bytes it reads using the key read above.
            _instructionDecoder.SMCTrampolineKey = smcKey;
            _instructionDecoder.CurrentKey = currentState.Key;
            
            ulong smcTrampolineStart = _instructionDecoder.ReaderOffset;
            
            // The following code decides whether the current block is an SMC trampoline by
            // attempting to decrypt a couple of instructions using the key byte read before.
            // If trying to read instructions using the SMC key yields in garbage data, we
            // can safely assume that the current block is not an SMC trampoline.
            // The code relies on the assumption that the first non NOP instructions in the
            // SMC trampoline block are part of an XOR operation and that the SMC trampoline
            // ends in an uncoditional jump.
            
            ILInstruction currentInstr;
            // Vanilla KoiVM SMC trampolines start with a double NOP.
            do
            {
                if (!_instructionDecoder.TryReadNextInstruction(out currentInstr))
                    return false;
            }
            while (currentInstr.OpCode.Code == ILCode.NOP);

            // The next instructions are part of a XOR operation, try to match the first two to make sure our key is valid.
            if (currentInstr.OpCode.Code != ILCode.PUSHR_DWORD || (VMRegisters)currentInstr.Operand != VMRegisters.BP)
                return false;

            if (!_instructionDecoder.TryReadNextInstruction(out currentInstr) || currentInstr.OpCode.Code != ILCode.PUSHI_DWORD)
                return false;

            // A SMC trampoline always ends with a JMP instruction, try to decode instructions until we find it.
            // Second condition of the loop is here to prevent reading too much. The SMC trampoline block is 170 bytes
            // long in Vanilla KoiVM. We use a maximum of 200 to account for modified KoiVM versions which might add,
            // for example, extra NOPs.
            while (_instructionDecoder.TryReadNextInstruction(out currentInstr) && _instructionDecoder.ReaderOffset - smcTrampolineStart <= 200)
            {
                if (currentInstr.OpCode.Code != ILCode.JMP)
                    continue;
                smcTrampolineEnd = _instructionDecoder.ReaderOffset;
                break;
            }

            // If the loop above exited without finding a JMP instruction, smcTrampolineEnd will be 0.
            return smcTrampolineEnd != 0;
        }
    }
}