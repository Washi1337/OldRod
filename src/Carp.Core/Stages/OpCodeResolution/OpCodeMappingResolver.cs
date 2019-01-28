using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using Carp.Core.Architecture;

namespace Carp.Core.Stages.OpCodeResolution
{
    public class OpCodeMappingResolver : IStage
    {
        public const string Tag = "MappingResolver";
        private static readonly SignatureComparer Comparer = new SignatureComparer();
        
        public string Name => "OpCode mapping resolver";
        
        public void Run(DevirtualisationContext context)
        {
            var rawConstants = ReadConstants(context);
            context.OpCodeMapping = ResolveOpCodeLookupTable(context, rawConstants);
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

        private static OpCodeMapping ResolveOpCodeLookupTable(DevirtualisationContext context, IDictionary<FieldDefinition, byte> rawConstants)
        {
            context.Logger.Debug(Tag, "Locating opcode interface...");
            var infos = LocateOpCodeInterfaces(context);
            if (infos.Count == 0)
                throw new DevirtualisationException("Could not locate opcode interfaces.");
            context.Logger.Debug(Tag,
                $"Opcode interfaces found ({string.Join(", ", infos.Select(x => x.InterfaceType.MetadataToken))}).");

            context.Logger.Debug(Tag, "Resolving opcode lookup table...");
            return MatchOpCodeTypes(context, rawConstants, infos);
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

        private static OpCodeMapping MatchOpCodeTypes(DevirtualisationContext context,
            IDictionary<FieldDefinition, byte> rawOpCodeFields,
            IList<OpCodeInterfaceInfo> opcodeInterfaces)
        {
            // There are two types of opcodes: normal opcodes and vcall opcodes.
            // We do not know yet which of the interfaces is the IOpcode and IVcall interface yet. They have exactly
            // the same members, however the amount of types implementing the IVCall interface is significantly lower
            // than normal opcodes. We determine therefore which one is which by checking the counts.
            
            var mapping1 = new Dictionary<byte, TypeDefinition>();
            var mapping2 = new Dictionary<byte, TypeDefinition>();

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
                        mapping1.Add(rawOpCodeFields[rawOpCodeField], opcodeType);
                    else
                        mapping2.Add(rawOpCodeFields[rawOpCodeField], opcodeType);
                }
            }

            if (mapping1.Count < mapping2.Count)
                (mapping1, mapping2) = (mapping2, mapping1);

            var opcodes = new Dictionary<byte, OpCodeInfo>();
            int currentCode = (int) ILOpCode.NOP;
            foreach (var entry in mapping1.OrderBy(e =>
                ((FieldDefinition) e.Value.Methods.First(x => x.Signature.Parameters.Count == 0)
                    .CilMethodBody.Instructions.First(x => x.OpCode.Code == CilCode.Ldsfld).Operand)
                .MetadataToken.ToUInt32()))
            {
                opcodes.Add(entry.Key, new OpCodeInfo(entry.Value, (ILOpCode) currentCode));
                currentCode++;
            }

            return new OpCodeMapping(opcodes, mapping2);
        }
        
        
    }
}