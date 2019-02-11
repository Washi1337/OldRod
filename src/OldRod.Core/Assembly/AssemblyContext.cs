using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using Rivers;

namespace OldRod.Core.Assembly
{
    public class AssemblyContext
    {
        private readonly TypeDefinition _flagHelperType;

        private readonly VariableSignature _arg0;
        private readonly VariableSignature _arg1;
        private readonly VariableSignature _result;
        private readonly VariableSignature _fl;

        public AssemblyContext(MetadataImage targetImage, VMConstants constants, TypeDefinition flagHelperType)
        {
            TargetImage = targetImage;
            Constants = constants;
            _flagHelperType = flagHelperType;
            
            ReferenceImporter = new ReferenceImporter(targetImage);

            _arg0 = new VariableSignature(targetImage.TypeSystem.UInt32);
            _arg1 = new VariableSignature(targetImage.TypeSystem.UInt32);
            _result = new VariableSignature(targetImage.TypeSystem.UInt32);
            _fl = new VariableSignature(targetImage.TypeSystem.Byte);

            Variables.Add(_arg0);
            Variables.Add(_arg1);
            Variables.Add(_result);
            Variables.Add(_fl);
        }

        public MetadataImage TargetImage
        {
            get;
        }

        public ReferenceImporter ReferenceImporter
        {
            get;
        }

        public VMConstants Constants
        {
            get;
        }

        public CilCodeGenerator CodeGenerator
        {
            get;
            set;
        }

        public IDictionary<Node, CilInstruction> BlockHeaders
        {
            get;
        } = new Dictionary<Node, CilInstruction>();

        public ICollection<VariableSignature> Variables
        {
            get;
        } = new List<VariableSignature>();
        
        public IEnumerable<CilInstruction> BuildBinaryExpression(
            IEnumerable<CilInstruction> op0,
            IEnumerable<CilInstruction> op1,
            IEnumerable<CilInstruction> @operator,
            byte mask)
        {
            var result = new List<CilInstruction>();

            result.AddRange(op0);
            result.Add(CilInstruction.Create(CilOpCodes.Stloc, _arg0));
            result.AddRange(op1);
            result.Add(CilInstruction.Create(CilOpCodes.Stloc, _arg1));
            
            result.Add(CilInstruction.Create(CilOpCodes.Ldloc, _arg0));
            result.Add(CilInstruction.Create(CilOpCodes.Ldloc, _arg1));
            result.AddRange(@operator);
            result.Add(CilInstruction.Create(CilOpCodes.Stloc, _result));

            var updateFl = _flagHelperType.Methods.First(x =>
                x.Name == "UpdateFL" 
                && x.Signature.Parameters[0].ParameterType.IsTypeOf("System", "UInt32"));

            result.AddRange(new[]
            {
                CilInstruction.Create(CilOpCodes.Ldloc, _arg0),
                CilInstruction.Create(CilOpCodes.Ldloc, _arg1),
                CilInstruction.Create(CilOpCodes.Ldloc, _result),
                CilInstruction.Create(CilOpCodes.Ldloc, _result),
                CilInstruction.Create(CilOpCodes.Ldloca, _fl),
                CilInstruction.Create(CilOpCodes.Ldc_I4, mask),
                CilInstruction.Create(CilOpCodes.Call, updateFl), 
            });
            
            return result;
        }
    }
}