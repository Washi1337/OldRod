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
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.PE.DotNet.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Disassembly.DataFlow;
using Rivers;

namespace OldRod.Core.CodeGen
{
    public class CodeGenerationContext
    {
        private readonly CilVariable _flagVariable;
        private readonly CilVariable _arg0;
        private readonly CilVariable _arg1;
        private readonly CilVariable _result;
        private bool _intermediateVariablesAdded;

        public CodeGenerationContext(CilMethodBody methodBody, VMConstants constants, CilVariable flagVariable,
            TypeDefinition flagHelperType)
        {
            MethodBody = methodBody;
            Constants = constants;
            _flagVariable = flagVariable;
            VmHelperType = flagHelperType;

            ReferenceImporter = new ReferenceImporter(TargetModule);

            _arg0 = new CilVariable("__arg0", TargetModule.CorLibTypeFactory.UInt32);
            _arg1 = new CilVariable("__arg1", TargetModule.CorLibTypeFactory.UInt32);
            _result = new CilVariable("__result", TargetModule.CorLibTypeFactory.UInt32);
        }

        public ModuleDefinition TargetModule => MethodBody.Owner.Module;

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

        public TypeDefinition VmHelperType
        {
            get;
        }

        public IDictionary<Node, CilInstruction> BlockHeaders
        {
            get;
        } = new Dictionary<Node, CilInstruction>();

        public IDictionary<CilVariable, CilLocalVariable> Variables
        {
            get;
        } = new Dictionary<CilVariable, CilLocalVariable>();

        public IDictionary<CilParameter, Parameter> Parameters
        {
            get;
        } = new Dictionary<CilParameter, Parameter>();
        
        public IDictionary<EHFrame, CilExceptionHandler> ExceptionHandlers
        {
            get;
        } = new Dictionary<EHFrame, CilExceptionHandler>();

        private void EnsureIntermediateVariablesAdded()
        {
            if (!_intermediateVariablesAdded)
            {
                _intermediateVariablesAdded = true;
                Variables.Add(_arg0, new CilLocalVariable(_arg0.VariableType));
                Variables.Add(_arg1, new CilLocalVariable(_arg1.VariableType));
                Variables.Add(_result, new CilLocalVariable(_result.VariableType));
            }
        }
        
        public IEnumerable<CilInstruction> BuildFlagAffectingExpression32(
            IEnumerable<CilInstruction> argument,
            IEnumerable<CilInstruction> @operator,
            byte mask,
            bool pushResult = true)
        {  
            EnsureIntermediateVariablesAdded();

            var result = new List<CilInstruction>();
            
            result.AddRange(argument);
            result.Add(new CilInstruction(CilOpCodes.Stloc, Variables[_arg0]));

            result.Add(new CilInstruction(CilOpCodes.Ldloc, Variables[_arg0]));
            result.AddRange(@operator);
            result.Add(new CilInstruction(CilOpCodes.Stloc, Variables[_result]));

            var updateFl = VmHelperType.Methods.First(x =>
                x.Name == "UpdateFL"
                && x.Parameters[0].ParameterType.IsTypeOf("System", "UInt32"));

            result.AddRange(new[]
            {
                new CilInstruction(CilOpCodes.Ldloc, Variables[_arg0]),
                new CilInstruction(CilOpCodes.Ldloc, Variables[_arg0]),
                new CilInstruction(CilOpCodes.Ldloc, Variables[_result]),
                new CilInstruction(CilOpCodes.Ldloc, Variables[_result]),
                new CilInstruction(CilOpCodes.Ldloca, Variables[_flagVariable]),
                new CilInstruction(CilOpCodes.Ldc_I4, mask),
                new CilInstruction(CilOpCodes.Call, updateFl),
            });
            
            if (pushResult)
                result.Add(new CilInstruction(CilOpCodes.Ldloc, Variables[_result]));

            return result;
        }

        public IEnumerable<CilInstruction> BuildFlagAffectingExpression32(
            IEnumerable<CilInstruction> argument0,
            IEnumerable<CilInstruction> argument1,
            IEnumerable<CilInstruction> @operator,
            byte mask,
            bool invertedOrder = false,
            bool pushResult = true)
        {
            EnsureIntermediateVariablesAdded();
            
            var result = new List<CilInstruction>();

            result.AddRange(argument0);
            result.Add(new CilInstruction(CilOpCodes.Stloc, Variables[_arg0]));
            result.AddRange(argument1);
            result.Add(new CilInstruction(CilOpCodes.Stloc, Variables[_arg1]));

            result.Add(new CilInstruction(CilOpCodes.Ldloc, Variables[_arg0]));
            result.Add(new CilInstruction(CilOpCodes.Ldloc, Variables[_arg1]));
            result.AddRange(@operator);
            result.Add(new CilInstruction(CilOpCodes.Stloc, Variables[_result]));

            var updateFl = VmHelperType.Methods.First(x =>
                x.Name == "UpdateFL"
                && x.Parameters[0].ParameterType.IsTypeOf("System", "UInt32"));

            if (invertedOrder)
            {
                result.AddRange(new[]
                {
                    new CilInstruction(CilOpCodes.Ldloc, Variables[_result]),
                    new CilInstruction(CilOpCodes.Ldloc, Variables[_arg1]),
                    new CilInstruction(CilOpCodes.Ldloc, Variables[_arg0]),
                    new CilInstruction(CilOpCodes.Ldloc, Variables[_result]),
                });
            }
            else
            {
                result.AddRange(new[]
                {
                    new CilInstruction(CilOpCodes.Ldloc, Variables[_arg0]),
                    new CilInstruction(CilOpCodes.Ldloc, Variables[_arg1]),
                    new CilInstruction(CilOpCodes.Ldloc, Variables[_result]),
                    new CilInstruction(CilOpCodes.Ldloc, Variables[_result]),
                });
            }

            result.AddRange(new[]
            {
                new CilInstruction(CilOpCodes.Ldloca, Variables[_flagVariable]),
                new CilInstruction(CilOpCodes.Ldc_I4, mask),
                new CilInstruction(CilOpCodes.Call, updateFl),
            });
            
            if (pushResult)
                result.Add(new CilInstruction(CilOpCodes.Ldloc, Variables[_result]));

            return result;
        }
    }
}