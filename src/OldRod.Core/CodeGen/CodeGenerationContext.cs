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
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using Rivers;

namespace OldRod.Core.CodeGen
{
    public class CodeGenerationContext
    {
        private readonly VariableSignature _flagVariable;
        private readonly TypeDefinition _flagHelperType;

        private readonly VariableSignature _arg0;
        private readonly VariableSignature _arg1;
        private readonly VariableSignature _result;

        public CodeGenerationContext(CilMethodBody methodBody, VMConstants constants, VariableSignature flagVariable,
            TypeDefinition flagHelperType)
        {
            MethodBody = methodBody;
            Constants = constants;
            _flagVariable = flagVariable;
            _flagHelperType = flagHelperType;

            ReferenceImporter = new ReferenceImporter(TargetImage);

            _arg0 = new VariableSignature(TargetImage.TypeSystem.UInt32);
            _arg1 = new VariableSignature(TargetImage.TypeSystem.UInt32);
            _result = new VariableSignature(TargetImage.TypeSystem.UInt32);

            Variables.Add(_arg0);
            Variables.Add(_arg1);
            Variables.Add(_result);
        }

        public MetadataImage TargetImage => MethodBody.Method.Image;

        public CilMethodBody MethodBody
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

        public IEnumerable<CilInstruction> BuildFlagAffectingExpression(
            IEnumerable<CilInstruction> argument0,
            IEnumerable<CilInstruction> argument1,
            IEnumerable<CilInstruction> @operator,
            byte mask,
            bool invertedOrder = false)
        {
            var result = new List<CilInstruction>();

            result.AddRange(argument0);
            result.Add(CilInstruction.Create(CilOpCodes.Stloc, _arg0));
            result.AddRange(argument1);
            result.Add(CilInstruction.Create(CilOpCodes.Stloc, _arg1));

            result.Add(CilInstruction.Create(CilOpCodes.Ldloc, _arg0));
            result.Add(CilInstruction.Create(CilOpCodes.Ldloc, _arg1));
            result.AddRange(@operator);
            result.Add(CilInstruction.Create(CilOpCodes.Stloc, _result));

            var updateFl = _flagHelperType.Methods.First(x =>
                x.Name == "UpdateFL"
                && x.Signature.Parameters[0].ParameterType.IsTypeOf("System", "UInt32"));

            if (invertedOrder)
            {
                result.AddRange(new[]
                {
                    CilInstruction.Create(CilOpCodes.Ldloc, _result),
                    CilInstruction.Create(CilOpCodes.Ldloc, _arg0),
                    CilInstruction.Create(CilOpCodes.Ldloc, _arg1),
                    CilInstruction.Create(CilOpCodes.Ldloc, _result),
                });
            }
            else
            {
                result.AddRange(new[]
                {
                    CilInstruction.Create(CilOpCodes.Ldloc, _arg0),
                    CilInstruction.Create(CilOpCodes.Ldloc, _arg1),
                    CilInstruction.Create(CilOpCodes.Ldloc, _result),
                    CilInstruction.Create(CilOpCodes.Ldloc, _result),
                });
            }

            result.AddRange(new[]
            {
                CilInstruction.Create(CilOpCodes.Ldloca, _flagVariable),
                CilInstruction.Create(CilOpCodes.Ldc_I4, mask),
                CilInstruction.Create(CilOpCodes.Call, updateFl),
            });

            return result;
        }
    }
}