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
using AsmResolver.Net.Signatures;
using OldRod.Core.Ast.Cil;

namespace OldRod.Core.Recompiler.Transform
{
    public class TypeInference : ChangeAwareCilAstTransform
    {
        private static readonly SignatureComparer Comparer = new SignatureComparer();
        
        private TypeHelper _helper;
        private RecompilerContext _context;
        
        public override string Name => "Type Inference";

        public override bool ApplyTransformation(RecompilerContext context, CilCompilationUnit unit)
        {
            _context = context;
            _helper = new TypeHelper(context.ReferenceImporter);
            return base.ApplyTransformation(context, unit);
        }

        public override bool VisitCompilationUnit(CilCompilationUnit unit)
        {
            bool changed = false;
            
            // Go over each variable, and figure out the common base type of all the values that are assigned to it.
            // This is the new variable type.
            foreach (var variable in unit.Variables.Where(x => x.UsedBy.Count > 0))
                changed |= TryInferVariableType(variable);

            foreach (var parameter in unit.Parameters.Where(x => x.UsedBy.Count > 0 && !x.HasFixedType))
                changed |= TryInferVariableType(parameter);

            return changed;
        }

        private bool TryInferVariableType(CilVariable variable)
        {
            // Do not update the type of the flags variable.
            if (_context.FlagVariable == variable)
                return false;
            
            // Collect expected types.
            var expectedTypes = CollectExpectedTypes(variable);

            ITypeDescriptor newVariableType = null;
            
            if (expectedTypes.Any(t => t.IsTypeOf("System", "Array")))
                newVariableType = TryInferArrayType(variable);

            if (newVariableType == null) 
                newVariableType = _helper.GetCommonBaseType(expectedTypes);

            return TrySetVariableType(variable, newVariableType);
        }

        private ICollection<ITypeDescriptor> CollectExpectedTypes(CilVariable variable)
        {
            var expectedTypes = new List<ITypeDescriptor>();
            foreach (var use in variable.UsedBy)
            {
                var expectedType = use.ExpectedType;
                if (expectedType == null)
                    continue;
                
                if (!use.IsReference)
                {
                    // Normal read reference to the variable (e.g. using a ldloc or ldarg).
                    expectedTypes.Add(expectedType);
                }
                else if (expectedType is ByReferenceTypeSignature byRefType)
                {
                    // The variable's address was used (e.g. using a ldloca or ldarga). To avoid the type inference 
                    // to think that the variable is supposed to be a byref type, we get the base type instead.
                    expectedTypes.Add(byRefType.BaseType);
                }
                else
                {
                    // If this happens, we probably have an error somewhere in an earlier stage of the recompiler.
                    // Variable loaded by reference should always have a byref type sig as expected type. 

                    throw new RecompilerException(
                        $"Variable {use.Variable.Name} in the expression `{use.Parent}` in "
                        + $"{_context.MethodBody.Method.Name} ({_context.MethodBody.Method.MetadataToken}) was passed on " +
                        $"by reference, but does not have a by-reference expected type.");
                }
            }

            return expectedTypes;
        }

        private ITypeDescriptor TryInferArrayType(CilVariable variable)
        {
            if (variable.AssignedBy.Count == 0)
                return null;
            
            var types = variable.AssignedBy
                .Select(a => a.Value.ExpressionType)
                .ToArray();

            if (types[0] is SzArrayTypeSignature arrayType
                && types.All(t => Comparer.Equals(t, arrayType)))
            {
                return arrayType;
            }

            return null;
        }

        private bool TrySetVariableType(CilVariable variable, ITypeDescriptor variableType)
        {
            if (variableType != null && variable.VariableType.FullName != variableType.FullName)
            {
                var newType = _context.TargetImage.TypeSystem.GetMscorlibType(variableType)
                              ?? _context.ReferenceImporter.ImportTypeSignature(variableType.ToTypeSignature());
                variable.VariableType = newType;

                // Update the expression type of all references to the variable.
                foreach (var use in variable.UsedBy)
                {
                    use.ExpressionType = use.IsReference
                        ? new ByReferenceTypeSignature(newType)
                        : newType;
                }

                // Update the expected type of all expressions that are assigned to the variable.
                foreach (var assign in variable.AssignedBy)
                    assign.Value.ExpectedType = newType;

                return true;
            }

            return false;
        }
    }
}