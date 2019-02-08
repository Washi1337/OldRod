using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast;
using Rivers;

namespace OldRod.Core.Recompiler
{
    public class CompilerContext
    {
        private readonly TypeDefinition _flagHelperType;

        private readonly ILVariable _arg0 = new ILVariable("arg0");
        private readonly ILVariable _arg1 = new ILVariable("arg1");
        private readonly ILVariable _result = new ILVariable("result");
        private ILVariable _fl;

        public CompilerContext(MetadataImage targetImage, VMConstants constants, TypeDefinition flagHelperType)
        {
            TargetImage = targetImage;
            Constants = constants;
            _flagHelperType = flagHelperType;
            
            ReferenceImporter = new ReferenceImporter(targetImage);

            Variables[_arg0] = new VariableSignature(targetImage.TypeSystem.UInt32);
            Variables[_arg1] = new VariableSignature(targetImage.TypeSystem.UInt32);
            Variables[_result] = new VariableSignature(targetImage.TypeSystem.UInt32);
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

        public ILAstToCilVisitor CodeGenerator
        {
            get;
            set;
        }

        public IDictionary<Node, CilInstruction> BlockHeaders
        {
            get;
        } = new Dictionary<Node, CilInstruction>();

        public IDictionary<ILVariable, VariableSignature> Variables
        {
            get;
        } = new Dictionary<ILVariable, VariableSignature>();
        
        public IEnumerable<CilInstruction> BuildBinaryExpression(
            IEnumerable<CilInstruction> op0,
            IEnumerable<CilInstruction> op1,
            IEnumerable<CilInstruction> @operator,
            byte mask)
        {
            if (_fl == null) 
                _fl = Variables.Keys.First(x => x.Name == "FL");
            
            var result = new List<CilInstruction>();

            result.AddRange(op0);
            result.Add(CilInstruction.Create(CilOpCodes.Stloc, Variables[_arg0]));
            result.AddRange(op1);
            result.Add(CilInstruction.Create(CilOpCodes.Stloc, Variables[_arg1]));
            
            result.Add(CilInstruction.Create(CilOpCodes.Ldloc, Variables[_arg0]));
            result.Add(CilInstruction.Create(CilOpCodes.Ldloc, Variables[_arg1]));
            result.AddRange(@operator);
            result.Add(CilInstruction.Create(CilOpCodes.Stloc, Variables[_result]));

            var updateFl = _flagHelperType.Methods.First(x =>
                x.Name == "UpdateFL" 
                && x.Signature.Parameters[0].ParameterType.IsTypeOf("System", "UInt32"));

            result.AddRange(new[]
            {
                CilInstruction.Create(CilOpCodes.Ldloc, Variables[_arg0]),
                CilInstruction.Create(CilOpCodes.Ldloc, Variables[_arg1]),
                CilInstruction.Create(CilOpCodes.Ldloc, Variables[_result]),
                CilInstruction.Create(CilOpCodes.Ldloc, Variables[_result]),
                CilInstruction.Create(CilOpCodes.Ldloca, Variables[_fl]),
                CilInstruction.Create(CilOpCodes.Ldc_I4, mask),
                CilInstruction.Create(CilOpCodes.Call, updateFl), 
            });
            
            return result;
        }
    }
}