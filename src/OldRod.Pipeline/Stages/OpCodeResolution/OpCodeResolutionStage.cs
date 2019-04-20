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

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using OldRod.Core.Architecture;

namespace OldRod.Pipeline.Stages.OpCodeResolution
{
    public class OpCodeResolutionStage : IStage
    {
        public const string Tag = "MappingResolver";
        private static readonly SignatureComparer Comparer = new SignatureComparer();
        
        public string Name => "OpCode mapping resolution stage";
        
        public void Run(DevirtualisationContext context)
        {
            context.OpCodeMapping = ResolveOpCodeLookupTable(context);
        }

        private static OpCodeMapping ResolveOpCodeLookupTable(DevirtualisationContext context)
        {
            context.Logger.Debug(Tag, "Locating opcode interface...");
            var infos = LocateOpCodeInterfaces(context);
            if (infos.Count == 0)
                throw new DevirtualisationException("Could not locate opcode interfaces.");
            context.Logger.Debug(Tag,
                $"Opcode interfaces found ({string.Join(", ", infos.Select(x => x.InterfaceType.MetadataToken))}).");

            context.Logger.Debug(Tag, "Resolving opcode lookup table...");
            return MatchOpCodeTypes(context, infos);
        }

        private static IList<OpCodeInterfaceInfo> LocateOpCodeInterfaces(DevirtualisationContext context)
        {
            var result = new List<OpCodeInterfaceInfo>();
            foreach (var type in context.RuntimeImage.Assembly.Modules[0].TopLevelTypes.Where(x => x.IsInterface && x.Methods.Count == 2))
            {
                MethodDefinition getter = null;
                MethodDefinition run = null;
                
                foreach (var method in type.Methods)
                {
                    var signature = method.Signature;
                    
                    if (signature.Parameters.Count == 0 && signature.ReturnType.IsTypeOf("System", "Byte"))
                    {
                        // Matched signature byte get_Code(): 
                        getter = method;
                    }
                    else if (signature.Parameters.Count == 2
                             && (method.Parameters.FirstOrDefault(x => x.Sequence == 2)?.Attributes
                                     .HasFlag(ParameterAttributes.Out) ?? false)
                             && signature.ReturnType.IsTypeOf("System", "Void"))
                    {
                        // Matched signature of void Run(VMContext, out ExecutionStage).
                        run = method;
                    }
                }

                if (getter != null && run != null)
                    result.Add(new OpCodeInterfaceInfo(type, getter, run));
            }

            return result;
        }

        private static OpCodeMapping MatchOpCodeTypes(DevirtualisationContext context, IList<OpCodeInterfaceInfo> opcodeInterfaces)
        {
            // There are two types of opcodes: normal opcodes and vcall opcodes.
            // We do not know yet which of the interfaces is the IOpcode and IVcall interface yet. They have exactly
            // the same members, however the amount of types implementing the IVCall interface is significantly lower
            // than normal opcodes. We determine therefore which one is which by checking the counts.
            
            var mapping1 = new Dictionary<byte, TypeDefinition>();
            var mapping2 = new Dictionary<byte, TypeDefinition>();

            // Find all opcode and vcall classes.
            foreach (var opcodeType in context.RuntimeImage.Assembly.Modules[0].TopLevelTypes
                .Where(t => t.IsClass))
            {
                var opcodeInterface = opcodeInterfaces.FirstOrDefault(x =>
                    opcodeType.Interfaces.Any(i => i.Interface == x.InterfaceType));

                if (opcodeInterface != null)
                {
                    var getCode = opcodeType.Methods.First(x => x.Name == opcodeInterface.GetCodeMethod.Name);
                    var ldsfld = getCode.CilMethodBody.Instructions.First(x => x.OpCode.Code == CilCode.Ldsfld);
                    var rawOpCodeField = (FieldDefinition) ldsfld.Operand;
                    
                    if (opcodeInterface == opcodeInterfaces[0])
                        mapping1.Add(context.Constants.ConstantFields[rawOpCodeField], opcodeType);
                    else
                        mapping2.Add(context.Constants.ConstantFields[rawOpCodeField], opcodeType);
                }
            }

            // The biggest mapping is the one of the opcodes, the smallest is the vcalls.
            if (mapping1.Count < mapping2.Count)
                (mapping1, mapping2) = (mapping2, mapping1);

            // Map all opcodes.
            var opcodes = new Dictionary<byte, OpCodeInfo>();
            foreach (var entry in mapping1)
            {
                var field = (FieldDefinition) entry.Value.Methods.First(x => x.Signature.Parameters.Count == 0)
                    .CilMethodBody.Instructions.First(x => x.OpCode.Code == CilCode.Ldsfld).Operand;

                var opCode = (ILCode) Enum.Parse(typeof(ILCode), field.Name.Substring(field.Name.IndexOf('_')+1));

                if (context.Options.RenameConstants)
                {
                    entry.Value.Namespace = "KoiVM.Runtime.OpCodes";
                    entry.Value.Name = opCode.ToString();
                }

                opcodes.Add(entry.Key, new OpCodeInfo(entry.Value, opCode));
            }

            // Map all vcalls.
            foreach (var entry in mapping2)
            {
                var field = (FieldDefinition) entry.Value.Methods.First(x => x.Signature.Parameters.Count == 0)
                    .CilMethodBody.Instructions.First(x => x.OpCode.Code == CilCode.Ldsfld).Operand;

                if (context.Options.RenameConstants)
                {
                    entry.Value.Namespace = "KoiVM.Runtime.VCalls";
                    entry.Value.Name = field.Name.Substring(field.Name.IndexOf('_')+1);
                }
            }

            return new OpCodeMapping(opcodes, mapping2);
        }
        
        
    }
}