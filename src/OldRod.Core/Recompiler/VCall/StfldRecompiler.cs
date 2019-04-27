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

using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Recompiler.VCall
{
    public class StfldRecompiler : IVCallRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            var metadata = (FieldAnnotation) expression.Annotation;
            
            // Enter generic context.
            context.EnterMember(metadata.Field);
            
            var field = (FieldDefinition) metadata.Field.Resolve();

            // Construct CIL expression.
            var result = new CilInstructionExpression(field.IsStatic ? CilOpCodes.Stsfld : CilOpCodes.Stfld, metadata.Field);

            if (!field.IsStatic)
            {
                // Recompile object expression if field is an instance field.
                var objectExpression = (CilExpression) expression.Arguments[expression.Arguments.Count - 2]
                    .AcceptVisitor(context.Recompiler);
                objectExpression.ExpectedType = field.DeclaringType.ToTypeSignature()
                    .InstantiateGenericTypes(context.GenericContext);
                result.Arguments.Add(objectExpression);
            }

            // Recompile value.
            var valueExpression = (CilExpression) expression.Arguments[expression.Arguments.Count - 1]
                .AcceptVisitor(context.Recompiler);
            result.Arguments.Add(valueExpression);

            // Exit generic context.
            context.ExitMember();
            
            return result;
        }
        
    }
    
}