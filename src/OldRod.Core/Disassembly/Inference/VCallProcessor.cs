using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.DataFlow;
using OldRod.Core.Emulation;

namespace OldRod.Core.Disassembly.Inference
{
    public class VCallProcessor
    {
        private readonly MetadataImage _image;
        private readonly VMConstants _constants;
        private readonly KoiStream _koiStream;
        
        public VCallProcessor(MetadataImage image, VMConstants constants, KoiStream koiStream)
        {
            _image = image ?? throw new ArgumentNullException(nameof(image));
            _constants = constants ?? throw new ArgumentNullException(nameof(constants));
            _koiStream = koiStream ?? throw new ArgumentNullException(nameof(koiStream));
        }

        public IList<ProgramState> ProcessVCall(ILInstruction instruction, ProgramState next)
        {
            var nextStates = new List<ProgramState>(1);

            var symbolicVCallValue = next.Stack.Pop();
            instruction.Dependencies.Add(symbolicVCallValue);
            
            var emulator = new InstructionEmulator();
            emulator.EmulateDependentInstructions(instruction);
            var vcall = _constants.VMCalls[emulator.Stack.Pop().U1];
            
            switch (vcall)
            {
                case VMCalls.EXIT:
                    break;
                case VMCalls.BREAK:
                    break;
                case VMCalls.ECALL:
                    ProcessECall(instruction, next);
                    break;
                case VMCalls.CAST:
                    break;
                case VMCalls.CKFINITE:
                    break;
                case VMCalls.CKOVERFLOW:
                    break;
                case VMCalls.RANGECHK:
                    break;
                case VMCalls.INITOBJ:
                    break;
                case VMCalls.LDFLD:
                    break;
                case VMCalls.LDFTN:
                    break;
                case VMCalls.TOKEN:
                    break;
                case VMCalls.THROW:
                    break;
                case VMCalls.SIZEOF:
                    break;
                case VMCalls.STFLD:
                    break;
                case VMCalls.BOX:
                    ProcessBox(instruction, next);
                    break;
                case VMCalls.UNBOX:
                    break;
                case VMCalls.LOCALLOC:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (vcall != VMCalls.EXIT)
                nextStates.Add(next);

            return nextStates;
        }

        private void ProcessBox(ILInstruction instruction, ProgramState next)
        {
            // Pop arguments and add dependencies.
            var symbolicType = next.Stack.Pop();
            var symbolicValue = next.Stack.Pop();
            instruction.Dependencies.Add(symbolicValue);
            instruction.Dependencies.Add(symbolicType);
            next.Stack.Push(new SymbolicValue(instruction));
            
            // Infer type.
            var typeSlot = InferStackValue(symbolicType);
            var type = (ITypeDefOrRef) _image.ResolveMember(_koiStream.References[typeSlot.U4]);

            // Infer value.
            var valueSlot = InferStackValue(symbolicValue);            
            object value;
            if (type.IsTypeOf("System", "String"))
                value = _koiStream.Strings[valueSlot.U4];
            else
                value = null; // TODO: infer value types.
            
            // Add metadata
            instruction.InferredMetadata = new BoxMetadata(type, value);
        }

        private void ProcessECall(ILInstruction instruction, ProgramState next)
        {
            var symbolicMethod = next.Stack.Pop();
            instruction.Dependencies.Add(symbolicMethod);
            
            // Infer method and opcode used.
            var methodSlot = InferStackValue(symbolicMethod);
            var methodId = methodSlot.U4 & 0x3fffffff;
            var opCode = _constants.ECallOpCodes[(byte) (methodSlot.U4 >> 30)];
            var methodToken = _koiStream.References[methodId];
            var method = (IMethodDefOrRef) _image.ResolveMember(methodToken);

            // Add metadata
            instruction.InferredMetadata = new ECallMetadata(method, opCode);

            // Pop method arguments:
            var methodSignature = (MethodSignature) method.Signature;
            for (int i = methodSignature.Parameters.Count - 1; i >= 0; i--)
                instruction.Dependencies.Add(next.Stack.Pop());
            if (method.Signature.HasThis)
                instruction.Dependencies.Add(next.Stack.Pop());
        }

        private static VMSlot InferStackValue(SymbolicValue symbolicValue)
        {
            var emulator = new InstructionEmulator();
            var pushValue = symbolicValue.DataSources.First(); // TODO: might need to verify multiple data sources.
            emulator.EmulateDependentInstructions(pushValue);
            emulator.EmulateInstruction(pushValue);
            return emulator.Stack.Pop();
        }
    }
}