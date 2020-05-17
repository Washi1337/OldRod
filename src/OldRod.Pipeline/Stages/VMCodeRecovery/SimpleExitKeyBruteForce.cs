using System.Linq;
using System.Runtime.CompilerServices;
using AsmResolver;
using OldRod.Core;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Pipeline.Stages.VMCodeRecovery
{
    public class SimpleExitKeyBruteForce : IExitKeyResolver
    {
        public const string Tag = "ExitKeyBruteForce";
        public string Name => "Simple exit key brute-force";

        public uint? ResolveExitKey(ILogger logger, KoiStream koiStream, VMConstants constants, VMFunction function)
        {
            // Strategy:
            //
            // Find CALL references to the function, and try every possible key in the key space that 
            // decrypts the next instruction to either a PUSHR_xxxx R0 or a PUSHR_DWORD SP, as they
            // appear in the post-call to either store the return value, or adjust SP to clean up
            // the stack.
            
            // Since the actual decryption of the opcode only uses the 8 least significant bits, we can 
            // make an optimisation that shaves off around 8 bits of the key space. By attempting to 
            // decrypt the first byte using just the first 8 bits, and verifying whether this output
            // results to one of the instructions mentioned in the above, we can quickly rule out many
            // potential keys.
            
            var callReferences = function.References.Where(r => r.ReferenceType == FunctionReferenceType.Call).ToArray();
            if (callReferences.Length == 0)
            {
                logger.Warning(Tag, $"Cannot brute-force the exit key of function_{function.EntrypointAddress:X4} as it has no recorded call references.");
                return null;
            }

            var reader = koiStream.Contents.CreateReader();
            
            // Find any call reference.
            foreach (var callReference in callReferences)
            {
                var call = callReference.Caller.Instructions[callReference.Offset];
                long targetOffset = call.Offset + call.Size;
                reader.FileOffset = (uint) targetOffset;
                byte[] encryptedOpCodes = new byte[3];
                reader.ReadBytes(encryptedOpCodes, 0, encryptedOpCodes.Length);

                // Go over all possible LSBs.
                for (uint lsb = 0; lsb < byte.MaxValue; lsb++)
                {
                    // Check whether the LSB decodes to a PUSHR_xxxx.
                    if (IsPotentialLSB(constants, encryptedOpCodes[0], lsb))
                    {
                        // Go over all remaining 24 bits.
                        for (uint i = 0; i < 0x00FFFFFF; i++)
                        {
                            uint currentKey = (i << 8) | lsb;
                            
                            // Try new key.
                            if (IsValidKey(constants, encryptedOpCodes, currentKey))
                            {
                                // We have found a key!
                                return currentKey;
                            }
                        } // for all other 24 bits.
                    } // if potential LSB
                } // foreach LSB
            } // foreach call reference

            return null;
        }

        private static bool IsPotentialLSB(VMConstants constants, byte encryptedOpCode, uint lsb)
        {
            byte pushRDword = DecryptByte(encryptedOpCode, ref lsb);
            if (!constants.OpCodes.TryGetValue(pushRDword, out var opCode))
                return false;
            
            switch (opCode)
            {
                case ILCode.PUSHR_BYTE:
                case ILCode.PUSHR_WORD:
                case ILCode.PUSHR_DWORD:
                case ILCode.PUSHR_QWORD:
                case ILCode.PUSHR_OBJECT:
                    return true;
                
                default:
                    return false;
            }
        }

        private static bool IsValidKey(VMConstants constants, byte[] data, uint key)
        {
            // Opcode byte.
            byte pushRDword = DecryptByte(data[0], ref key);
            if (!constants.OpCodes.TryGetValue(pushRDword, out var opCode))
                return false;

            switch (opCode)
            {
                case ILCode.PUSHR_BYTE:
                case ILCode.PUSHR_WORD:
                case ILCode.PUSHR_DWORD:
                case ILCode.PUSHR_QWORD:
                case ILCode.PUSHR_OBJECT:
                    break;
                default:
                    return false;
            }

            // Fixup byte.
            byte fixup = DecryptByte(data[1], ref key);

            // Register operand.
            byte rawRegister = DecryptByte(data[2], ref key);
            if (!constants.Registers.TryGetValue(rawRegister, out var register))
                return false;
            
            switch (register)
            {
                case VMRegisters.R0:
                case VMRegisters.SP:
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte DecryptByte(byte encryptedByte, ref uint key)
        {
            byte b = (byte) (encryptedByte ^ key);
            key = key * 7 + b;
            return b;
        }
        
    }
}