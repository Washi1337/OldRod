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
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Ast.Cil;

namespace OldRod.Core.Recompiler.Transform
{
    public class TypeInference : ChangeAwareCilAstTransform
    {
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
            {
                var expectedTypes = variable.UsedBy.Select(use => use.ExpectedType).ToArray();
                var commonBaseType = _helper.GetCommonBaseType(expectedTypes);

                if (commonBaseType != null && variable.Signature.VariableType.FullName != commonBaseType.FullName)
                {
                    var newType = _context.TargetImage.TypeSystem.GetMscorlibType(commonBaseType) 
                                  ?? _context.ReferenceImporter.ImportTypeSignature(commonBaseType.ToTypeSignature());
                    variable.Signature.VariableType = newType;

                    // Update the expression type of all references to the variable.
                    foreach (var use in variable.UsedBy)
                        use.ExpressionType = newType;

                    // Update the expected type of all expressions that are assigned to the variable.
                    foreach (var assign in variable.AssignedBy)
                        assign.Value.ExpectedType = newType;
                    
                    changed = true;
                }
            }
            
            return changed;
        }
        
    }
}