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
using System.Linq;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using AsmResolver.Net.Signatures;
using OldRod.Core.Ast.Cil;

namespace OldRod.Core.Recompiler.Transform
{
    public class TypeConversionInsertion : ChangeAwareCilAstTransform
    {
        private TypeHelper _helper;
        private RecompilerContext _context;

        public override string Name => "Type Conversion Insertion";

        public override bool ApplyTransformation(RecompilerContext context, CilCompilationUnit unit)
        {
            _helper = new TypeHelper(context.ReferenceImporter);
            _context = context;
            return base.ApplyTransformation(context, unit);
        }

        public override bool VisitInstructionExpression(CilInstructionExpression expression)
        {
            if (expression.Instructions.Count == 1 
                && expression.Instructions[0].IsLdcI4
                && expression.Instructions[0].GetLdcValue() == 0
                && expression.ExpectedType != null 
                && !expression.ExpectedType.IsValueType)
            {
                expression.Instructions.Clear();
                expression.Instructions.Add(CilInstruction.Create(CilOpCodes.Ldnull));
                expression.ExpressionType = expression.ExpectedType;
                return true;
            }
            
            bool changed = base.VisitInstructionExpression(expression);

            foreach (var argument in expression.Arguments.ToArray())
                changed = EnsureTypeSafety(argument);

            return changed;
        }

        public override bool VisitAssignmentStatement(CilAssignmentStatement statement)
        {
            return base.VisitAssignmentStatement(statement) | EnsureTypeSafety(statement.Value);
        }

        private bool EnsureTypeSafety(CilExpression argument)
        {
            bool changed = false;
            if (!_helper.IsAssignableTo(argument.ExpressionType, argument.ExpectedType))
            {
                if (!argument.ExpressionType.IsValueType && argument.ExpectedType.IsValueType)
                {
                    // Reference type -> Value type.
                    if (argument.ExpressionType.IsTypeOf("System", "Object"))
                    {
                        UnboxAny(argument);
                        changed = true;
                    }
                }
                else if (!argument.ExpressionType.IsValueType && !argument.ExpectedType.IsValueType)
                {
                    // Reference type -> Reference type.
                    CastClass(argument);
                    changed = true;
                }
                else if (argument.ExpressionType.IsValueType && !argument.ExpectedType.IsValueType)
                {
                    // Value type -> Reference type.
                    var newArg = Box(argument);
                    if (!argument.ExpectedType.IsTypeOf("System", "Object"))
                        CastClass(newArg);

                    changed = true;
                }
                else if (argument.ExpressionType.IsValueType && argument.ExpectedType.IsValueType)
                {
                    // Value type -> Value type.
                    ConvertValueType(argument);
                    changed = true;
                }
            }

            return changed;
        }

        private CilExpression UnboxAny(CilExpression argument)
        {
            var newArgument = new CilInstructionExpression(CilOpCodes.Unbox_Any,
                _context.ReferenceImporter.ImportType(argument.ExpectedType.ToTypeDefOrRef()))
            {
                ExpectedType = argument.ExpectedType,
                ExpressionType = argument.ExpectedType,
            };   
            ReplaceArgument(argument, newArgument);
            
            return newArgument;
        }

        private CilExpression Box(CilExpression argument)
        {
            var newArgument = new CilInstructionExpression(CilOpCodes.Box,
                _context.ReferenceImporter.ImportType(argument.ExpectedType.ToTypeDefOrRef()))
            {
                ExpectedType = argument.ExpectedType,
                ExpressionType = _context.TargetImage.TypeSystem.Object,
            };
            ReplaceArgument(argument, newArgument);

            return newArgument;
        }

        private CilExpression CastClass(CilExpression argument)
        {
            var newArgument = new CilInstructionExpression(CilOpCodes.Castclass,
                _context.ReferenceImporter.ImportType(argument.ExpectedType.ToTypeDefOrRef()))
            {
                ExpectedType = argument.ExpectedType,
                ExpressionType = argument.ExpectedType,
            };
            ReplaceArgument(argument, newArgument);
            
            return newArgument;
        }

        private CilExpression ConvertValueType(CilExpression argument)
        {
            var corlibType = _context.TargetImage.TypeSystem.GetMscorlibType(argument.ExpectedType);
            if (corlibType == null)
            {
                var typeDef = (TypeDefinition) argument.ExpectedType.ToTypeDefOrRef().Resolve();

                // If the expected type is an enum, we might not even need the type conversion in the first place.
                // Check the enum underlying type if it's indeed the case.
                if (typeDef.IsEnum)
                {
                    var underlyingType = typeDef.GetEnumUnderlyingType();
                    if (argument.ExpressionType.FullName == underlyingType.FullName)
                    {
                        // Enum type is the same as the expression type, we don't need an explicit conversion.
                        argument.ExpressionType = argument.ExpectedType;
                        return argument;
                    }
                    
                    // Types still mismatch, we need the explicit conversion.
                    corlibType = _context.TargetImage.TypeSystem.GetMscorlibType(underlyingType);
                }
                
                if (corlibType == null)
                    throw new RecompilerException($"Conversion from value type {argument.ExpressionType} to value type {argument.ExpectedType} is not supported yet.");
            }
            
            CilOpCode code; 
            switch (corlibType.ElementType)
            {
                case ElementType.I1:
                    code = CilOpCodes.Conv_I1;
                    break;
                case ElementType.U1:
                    code = CilOpCodes.Conv_U1;
                    break;
                case ElementType.I2:
                    code = CilOpCodes.Conv_I2;
                    break;
                case ElementType.Char:
                case ElementType.U2:
                    code = CilOpCodes.Conv_U2;
                    break;
                case ElementType.Boolean:
                case ElementType.I4:
                    code = CilOpCodes.Conv_I4;
                    break;
                case ElementType.U4:
                    code = CilOpCodes.Conv_U4;
                    break;
                case ElementType.I8:
                    code = CilOpCodes.Conv_I8;
                    break;
                case ElementType.U8:
                    code = CilOpCodes.Conv_U8;
                    break;
                case ElementType.R4:
                    code = CilOpCodes.Conv_R4;
                    break;
                case ElementType.R8:
                    code = CilOpCodes.Conv_R8;
                    break;
                case ElementType.I:
                    code = CilOpCodes.Conv_I;
                    break;
                case ElementType.U:
                    code = CilOpCodes.Conv_U;
                    break;
                default:
                    throw new RecompilerException($"Conversion from value type {argument.ExpressionType} to value type {argument.ExpectedType} is not supported.");
            }
            
            var newArgument = new CilInstructionExpression(code)
            {
                ExpectedType = argument.ExpectedType,
                ExpressionType = argument.ExpectedType
            };
            ReplaceArgument(argument, newArgument);

            return newArgument;
        }

        private static void ReplaceArgument(CilExpression argument, CilInstructionExpression newArgument)
        {
            argument.ReplaceWith(newArgument);
            argument.ExpectedType = argument.ExpressionType;
            newArgument.Arguments.Add(argument);
        }
            
    }
}