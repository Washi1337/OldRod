using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
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

            var metadata = instruction.InferredMetadata as VCallMetadata;

            var symbolicVCallValue = next.Stack.Pop();
            instruction.Dependencies.AddOrMerge(0, symbolicVCallValue);
            var vcall = metadata?.VMCall ?? _constants.VMCalls[InferStackValue(symbolicVCallValue).U1];

            switch (vcall)
            {
                case VMCalls.ECALL:
                    ProcessECall(instruction, next);
                    break;
                case VMCalls.BOX:
                    ProcessBox(instruction, next);
                    break;
                case VMCalls.EXIT:
                case VMCalls.BREAK:
                case VMCalls.UNBOX:
                case VMCalls.LOCALLOC:
                case VMCalls.CAST:
                case VMCalls.CKFINITE:
                case VMCalls.CKOVERFLOW:
                case VMCalls.RANGECHK:
                case VMCalls.INITOBJ:
                case VMCalls.LDFLD:
                case VMCalls.LDFTN:
                case VMCalls.TOKEN:
                case VMCalls.THROW:
                case VMCalls.SIZEOF:
                case VMCalls.STFLD:
                    throw new NotSupportedException("VCALL " + vcall + " is not supported.");
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

            instruction.Dependencies.AddOrMerge(1, symbolicValue);
            instruction.Dependencies.AddOrMerge(2, symbolicType);

            next.Stack.Push(new SymbolicValue(instruction)
            {
                Type = VMType.Object
            });
            
            // Infer type.
            var typeSlot = InferStackValue(symbolicType);
            var type = (ITypeDefOrRef) _image.ResolveMember(_koiStream.References[typeSlot.U4]);

            // Infer value.
            
            // TODO: Box values pushed onto the stack might not be constants, but might be part of 
            //       a normal conversion of a stack value from value type to object. We need a way
            //       to detect this and encode this in the metadata somehow.
            //
            //       Perhaps by checking whether registers or stack slots are used in the dependent
            //       instructions?
            
            var valueSlot = InferStackValue(symbolicValue);            
            object value;
            if (type.IsTypeOf("System", "String"))
                value = _koiStream.Strings[valueSlot.U4];
            else
                value = null; // TODO: infer value types.

            
            // Add metadata
            instruction.InferredMetadata = new BoxMetadata(type, value)
            {
                InferredPopCount = instruction.Dependencies.Count
            };
        }

        private void ProcessECall(ILInstruction instruction, ProgramState next)
        {
            int index = 1;
            
            // Pop raw method value from stack.
            var symbolicMethod = next.Stack.Pop();
            instruction.Dependencies.AddOrMerge(index++, symbolicMethod);
            
            // Infer method and opcode used.
            var methodSlot = InferStackValue(symbolicMethod);
            uint methodId = methodSlot.U4 & 0x3fffffff;
            var opCode = _constants.ECallOpCodes[(byte) (methodSlot.U4 >> 30)];
            var method = (IMethodDefOrRef) _image.ResolveMember(_koiStream.References[methodId]);
            var methodSignature = (MethodSignature) method.Signature;

            // Collect method arguments:
            var arguments = new List<SymbolicValue>();
            for (int i = 0; i < methodSignature.Parameters.Count; i++)
                arguments.Add(next.Stack.Pop());
            if (method.Signature.HasThis)
                arguments.Add(next.Stack.Pop());
            
            arguments.Reverse();
            
            // Add argument dependencies.
            foreach (var argument in arguments)
                instruction.Dependencies.AddOrMerge(index++, argument);
            
            // Push result, if any.
            bool hasResult = methodSignature.ReturnType.IsTypeOf("System", "Void")
                             || opCode == VMECallOpCode.ECALL_NEWOBJ;
            if (!hasResult)
            {
                next.Stack.Push(new SymbolicValue(instruction)
                {
                    Type = methodSignature.ReturnType.ToVMType()
                });
            }

            // Add metadata
            instruction.InferredMetadata = new ECallMetadata(method, opCode)
            {
                InferredPopCount = instruction.Dependencies.Count,
                InferredPushCount = hasResult ? 0 : 1
            };
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