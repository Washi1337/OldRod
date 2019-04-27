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

using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Recompiler.VCall
{
    public class LdfldRecompiler : IVCallRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            var metadata = (FieldAnnotation) expression.Annotation;

            // Enter generic context for member.
            context.EnterMember(metadata.Field);
            
            var field = (FieldDefinition) metadata.Field.Resolve();

            // Select opcode and expression type.
            var expressionType = ((FieldSignature) metadata.Field.Signature).FieldType;
            CilOpCode opCode;
            if (metadata.IsAddress)
            {
                expressionType = new ByReferenceTypeSignature(expressionType);
                opCode = field.IsStatic ? CilOpCodes.Ldsflda : CilOpCodes.Ldflda;
            }
            else
            {
                opCode = field.IsStatic ? CilOpCodes.Ldsfld : CilOpCodes.Ldfld;
            }

            // Construct CIL expression.
            var result = new CilInstructionExpression(opCode, metadata.Field)
            {
                ExpressionType = expressionType.InstantiateGenericTypes(context.GenericContext)
            };

            if (!field.IsStatic)
            {
                // Recompile object expression if field is an instance field.
                var objectExpression = (CilExpression) expression.Arguments[expression.Arguments.Count - 1]
                    .AcceptVisitor(context.Recompiler);
                objectExpression.ExpectedType = field.DeclaringType.ToTypeSignature()
                    .InstantiateGenericTypes(context.GenericContext);
                result.Arguments.Add(objectExpression);
            }
            
            // Leave generic context.
            context.ExitMember();
            
            return result;
        }
        
    }
}