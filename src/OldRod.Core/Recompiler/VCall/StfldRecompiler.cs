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

using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Annotations;

namespace OldRod.Core.Recompiler.VCall
{
    public class StfldRecompiler : IVCallRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            var metadata = (FieldAnnotation) expression.Annotation;
            var fieldType = metadata.Field.Signature.FieldType;
            
            // Enter generic context.
            context.EnterMember(metadata.Field);

            bool hasThis = metadata.Field.Signature.HasThis;

            // Construct CIL expression.
            var result = new CilInstructionExpression(hasThis ? CilOpCodes.Stfld : CilOpCodes.Stsfld, metadata.Field);

            if (hasThis)
            {
                // Recompile object expression if field is an instance field.
                var objectExpression = (CilExpression) expression.Arguments[expression.Arguments.Count - 2]
                    .AcceptVisitor(context.Recompiler);
                
                var objectType = metadata.Field.DeclaringType
                    .ToTypeSignature()
                    .InstantiateGenericTypes(context.GenericContext);
                
                // Struct members can only be accessed when the object is passed on by reference.
                if (metadata.Field.DeclaringType.IsValueType)
                    objectType = new ByReferenceTypeSignature(objectType);

                objectExpression.ExpectedType = objectType;
                result.Arguments.Add(objectExpression);
            }

            // Recompile value.
            var valueExpression = (CilExpression) expression.Arguments[expression.Arguments.Count - 1]
                .AcceptVisitor(context.Recompiler);
            valueExpression.ExpectedType = fieldType.InstantiateGenericTypes(context.GenericContext);
            result.Arguments.Add(valueExpression);

            // Exit generic context.
            context.ExitMember();
            
            return result;
        }
        
    }
    
}