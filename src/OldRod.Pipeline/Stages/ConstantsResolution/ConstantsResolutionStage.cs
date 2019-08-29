// Project OldRod - A KoiVM devirtualisation utility.
// Copyright (C) 2019 Washi
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;

namespace OldRod.Pipeline.Stages.ConstantsResolution
{
    public class ConstantsResolutionStage : IStage
    {
        private const string Tag = "ConstantsResolver";

        public string Name => "Constants resolution stage";

        public void Run(DevirtualisationContext context)
        {
            bool rename = context.Options.RenameSymbols;
            
            var constants = new VMConstants();
            var fields = ReadConstants(context);
            
            foreach (var field in fields)
                constants.ConstantFields.Add(field.Key, field.Value);
         
            // TODO:
            // We assume that the constants appear in the same order as they were defined in the original source code.
            // This means the metadata tokens of the fields are also in increasing order. However, this could cause
            // problems when a fork of the obfuscation tool is made which scrambles the order.  A more robust way of
            // matching should be done that is order agnostic.
            
            var sortedFields = fields
                .OrderBy(x => x.Key.MetadataToken.ToUInt32())
                .ToArray();

            int currentIndex = 0;

            context.Logger.Debug2(Tag, "Resolving register mapping...");
            for (int i = 0; i < (int) VMRegisters.Max; i++, currentIndex++)
            {
                constants.Registers.Add(sortedFields[currentIndex].Value, (VMRegisters) i);
                if (rename)
                    sortedFields[currentIndex].Key.Name = "REG_" + (VMRegisters) i;
            }

            context.Logger.Debug2(Tag, "Resolving flag mapping...");
            for (int i = 1; i < (int) VMFlags.Max; i <<= 1, currentIndex++)
            {
                constants.Flags.Add(sortedFields[currentIndex].Value, (VMFlags) i);
                if (rename)
                    sortedFields[currentIndex].Key.Name = "FLAG_" + (VMFlags) i;
            }
            
            context.Logger.Debug2(Tag, "Resolving opcode mapping...");
            for (int i = 0; i < (int) ILCode.Max; i++, currentIndex++)
            {
                constants.OpCodes.Add(sortedFields[currentIndex].Value, (ILCode) i);
                if (rename)
                    sortedFields[currentIndex].Key.Name = "OPCODE_" + (ILCode) i;
            }

            context.Logger.Debug2(Tag, "Resolving vmcall mapping...");
            for (int i = 0; i < (int) VMCalls.Max; i++, currentIndex++)
            {
                constants.VMCalls.Add(sortedFields[currentIndex].Value, (VMCalls) i);
                if (rename)
                    sortedFields[currentIndex].Key.Name = "VMCALL_" + (VMCalls) i;
            }

            context.Logger.Debug2(Tag, "Resolving helper init ID...");
            if (rename)
                sortedFields[currentIndex].Key.Name = "HELPER_INIT";
            constants.HelperInit = sortedFields[currentIndex++].Value;
            
            context.Logger.Debug2(Tag, "Resolving ECall mapping...");
            for (int i = 0; i < 4; i++, currentIndex++)
            {
                constants.ECallOpCodes.Add(sortedFields[currentIndex].Value, (VMECallOpCode) i);
                if (rename)
                    sortedFields[currentIndex].Key.Name = "ECALL_" + (VMECallOpCode) i;
            }

            context.Logger.Debug2(Tag, "Resolving function signature flags...");
            sortedFields[currentIndex].Key.Name = "FLAG_INSTANCE";
            constants.FlagInstance = sortedFields[currentIndex++].Value;

            context.Logger.Debug2(Tag, "Resolving exception handler types...");
            for (int i = 0; i < (int) EHType.Max; i++, currentIndex++)
            {
                constants.EHTypes.Add(sortedFields[currentIndex].Value, (EHType) i);
                if (rename)
                    sortedFields[currentIndex].Key.Name = "EH_" + (EHType) i;   
            }
            
            context.Constants = constants;
        }
        
        private IDictionary<FieldDefinition, byte> ReadConstants(DevirtualisationContext context)
        {
            context.Logger.Debug(Tag, "Locating constants type...");
            var constantsType = LocateConstantsType(context);
            if (constantsType == null)
                throw new DevirtualisationException("Could not locate constants type!");
            context.Logger.Debug(Tag, $"Found constants type ({constantsType.MetadataToken}).");

            if (context.Options.RenameSymbols)
            {
                constantsType.Namespace = "KoiVM.Runtime.Dynamic";
                constantsType.Name = "Constants";
            }
            
            context.Logger.Debug(Tag, $"Resolving constants table...");
            return ParseConstantValues(context, constantsType);
        }

        private static TypeDefinition LocateConstantsType(DevirtualisationContext context)
        {
            TypeDefinition constantsType = null;
            
            if (context.Options.OverrideVMConstantsToken)
            {
                context.Logger.Debug(Tag, $"Using token {context.Options.VMConstantsToken} for constants type.");
                constantsType = (TypeDefinition) context.RuntimeImage.ResolveMember(context.Options.VMConstantsToken.Value);
            }
            else
            {
                // Constants type contains a lot of public static byte fields, and only those byte fields. 
                // Therefore we pattern match on this signature, by finding the type with the most public
                // static byte fields.

                // It is unlikely that any other type has that many byte fields, although it is possible.
                // This could be improved later on.

                int max = 0;
                foreach (var type in context.RuntimeImage.Assembly.Modules[0].TopLevelTypes)
                {
                    // Count public static byte fields.
                    int byteFields = type.Fields.Count(x =>
                        x.IsPublic && x.IsStatic && x.Signature.FieldType.IsTypeOf("System", "Byte"));

                    if (byteFields == type.Fields.Count && max < byteFields)
                    {
                        constantsType = type;
                        max = byteFields;
                    }
                }
            }

            return constantsType;
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