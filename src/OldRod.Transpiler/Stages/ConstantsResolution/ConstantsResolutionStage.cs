using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;

namespace OldRod.Transpiler.Stages.ConstantsResolution
{
    public class ConstantsResolutionStage : IStage
    {
        public const string Tag = "ConstantsResolver";

        public string Name => "Constants resolution stage";

        public void Run(DevirtualisationContext context)
        {
            var constants = new VMConstants();
            var fields = ReadConstants(context);
            
            foreach (var field in fields)
                constants.ConstantFields.Add(field.Key, field.Value);
            
            var sortedFields = fields
                .OrderBy(x => x.Key.MetadataToken.ToUInt32())
                .ToArray();

            int currentIndex = 0;

            context.Logger.Debug(Tag, "Resolving register mapping...");
            for (int i = 0; i < (int) VMRegisters.Max; i++, currentIndex++)
                constants.Registers.Add(sortedFields[currentIndex].Value, (VMRegisters) i);
            
            context.Logger.Debug(Tag, "Resolving flag mapping...");
            for (int i = 0; i < (int) VMFlags.Max; i++, currentIndex++)
                constants.Flags.Add(sortedFields[currentIndex].Value, (VMFlags) i);
            
            context.Logger.Debug(Tag, "Resolving opcode mapping...");
            for (int i = 0; i < (int) ILCode.Max; i++, currentIndex++)
                constants.OpCodes.Add(sortedFields[currentIndex].Value, (ILCode) i);
            
            context.Logger.Debug(Tag, "Resolving vmcall mapping...");
            for (int i = 0; i < (int) VMCalls.Max; i++, currentIndex++)
                constants.VMCalls.Add(sortedFields[currentIndex].Value, (VMCalls) i);

            // HELPER_INIT
            currentIndex++;
            
            context.Logger.Debug(Tag, "Resolving ECall mapping...");
            for (int i = 0; i < 4; i++, currentIndex++)
                constants.ECallOpCodes.Add(sortedFields[currentIndex].Value, (VMECallOpCode) i);

            context.Constants = constants;
        }
        
        private IDictionary<FieldDefinition, byte> ReadConstants(DevirtualisationContext context)
        {
            context.Logger.Debug(Tag, "Locating constants type...");
            var constantsType = LocateConstantsType(context);
            if (constantsType == null)
                throw new DevirtualisationException("Could not locate constants type!");
            context.Logger.Debug(Tag, $"Found constants type ({constantsType.MetadataToken}).");
            
            context.Logger.Debug(Tag, $"Resolving constants table...");
            return ParseConstantValues(context, constantsType);
        }

        private static TypeDefinition LocateConstantsType(DevirtualisationContext context)
        {
            // Constants type contains a lot of public static byte fields, and only those byte fields. 
            // Therefore we pattern match on this signature, by finding the type with the most public
            // static byte fields.
            
            // It is unlikely that any other type has that many byte fields, although it is possible.
            // This could be improved later on.
            
            TypeDefinition opcodesType = null;
            int max = 0;
            foreach (var type in context.RuntimeImage.Assembly.Modules[0].TopLevelTypes)
            {
                // Count public static byte fields.
                int byteFields = type.Fields.Count(x =>
                    x.IsPublic && x.IsStatic && x.Signature.FieldType.IsTypeOf("System", "Byte"));

                if (byteFields == type.Fields.Count && max < byteFields)
                {
                    opcodesType = type;
                    max = byteFields;
                }
            }
            
            return opcodesType;
        }

        private static IDictionary<FieldDefinition, byte> ParseConstantValues(DevirtualisationContext context, TypeDefinition opcodesType)
        {
            // .cctor initialises the fields using a repetition of the following sequence:
            //
            //     ldnull
            //     ldc.i4 x
            //     stfld constantfield
            //
            // We can simply go over each instruction and "emulate" the ldc.i4 and stfld instructions.
            
            var result = new Dictionary<FieldDefinition, byte>();
            var cctor = opcodesType.Methods.First(x => x.Name == ".cctor");

            byte nextValue = 0;
            foreach (var instruction in cctor.CilMethodBody.Instructions)
            {
                if (instruction.IsLdcI4)
                    nextValue = (byte) instruction.GetLdcValue();
                else if (instruction.OpCode.Code == CilCode.Stfld)
                    result[(FieldDefinition) instruction.Operand] = nextValue;
            }

            return result;
        }
    }
}