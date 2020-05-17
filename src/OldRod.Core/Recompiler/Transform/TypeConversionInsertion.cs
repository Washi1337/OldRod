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

using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using OldRod.Core.Ast.Cil;

namespace OldRod.Core.Recompiler.Transform
{
    public class TypeConversionInsertion : ChangeAwareCilAstTransform
    {
        private RecompilerContext _context;

        public override string Name => "Type Conversion Insertion";

        public override bool ApplyTransformation(RecompilerContext context, CilCompilationUnit unit)
        {
            _context = context;
            return base.ApplyTransformation(context, unit);
        }

        public override bool VisitInstructionExpression(CilInstructionExpression expression)
        {
            // KoiVM emits pushi_dword or pushi_qwords not only for pushing integers, but also for pushing null or 
            // floating point numbers as well. 
            if (TryOptimizeLdcI(expression))
                return true;
            
            // Insert conversions in all arguments.
            bool changed = base.VisitInstructionExpression(expression);

            // Ensure type safety for all processed arguments. 
            foreach (var argument in expression.Arguments.ToArray())
                changed = EnsureTypeSafety(argument);

            return changed;
        }

        public override bool VisitAssignmentStatement(CilAssignmentStatement statement)
        {
            return base.VisitAssignmentStatement(statement) | EnsureTypeSafety(statement.Value);
        }

        private static unsafe bool TryOptimizeLdcI(CilInstructionExpression expression)
        {
            if (expression.Instructions.Count != 1 || expression.ExpectedType == null)
                return false;

            var instruction = expression.Instructions[0];
            
            if (instruction.IsLdcI4())
            {
                int i4Value = instruction.GetLdcI4Constant();
                if (!expression.ExpectedType.IsValueType)
                {
                    if (i4Value == 0)
                    {
                        // If ldc.i4.0 and expected type is a ref type, the ldc.i4.0 pushes null. We can therefore
                        // optimize to ldnull.
                        ReplaceWithSingleInstruction(expression, new CilInstruction(CilOpCodes.Ldnull));
                        return true;
                    }
                }
                else if (expression.ExpectedType.IsTypeOf("System", "Single"))
                {
                    // KoiVM pushes floats using the pushi_dword instruction. Convert to ldc.r4 if a float is expected
                    // but an ldc.i4 instruction is pushing the value.
                    float actualValue = *(float*) &i4Value;
                    ReplaceWithSingleInstruction(expression, new CilInstruction(CilOpCodes.Ldc_R4, actualValue));
                    return true;
                }
            }
            else if (instruction.OpCode.Code == CilCode.Ldc_I8 && expression.ExpectedType.IsTypeOf("System", "Double"))
            {
                // KoiVM pushes doubles using the pushi_qword instruction. Convert to ldc.r8 if a double is expected
                // but an ldc.i8 instruction is pushing the value.
                long i8Value = (long) instruction.Operand;
                double actualValue = *(double*) &i8Value;
                ReplaceWithSingleInstruction(expression, new CilInstruction(CilOpCodes.Ldc_R8, actualValue));
                return true;
            }

            return false;
        }

        private static void ReplaceWithSingleInstruction(CilInstructionExpression expression, CilInstruction newInstruction)
        {
            expression.Instructions.Clear();
            expression.Instructions.Add(newInstruction);
            expression.ExpressionType = expression.ExpectedType;
        }

        private bool EnsureTypeSafety(CilExpression argument)
        {
            bool changed = false;
            
            if (!_context.TypeHelper.IsAssignableTo(argument.ExpressionType, argument.ExpectedType))
            {
                if (!argument.ExpressionType.IsValueType && argument.ExpectedType.IsValueType)
                {
                    // Reference type -> Value type.
                    changed = ConvertRefTypeToValueType(argument);
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
                    if (!newArg.ExpectedType.IsTypeOf("System", "Object"))
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

        private bool ConvertRefTypeToValueType(CilExpression argument)
        {
            if (argument.ExpressionType.IsTypeOf("System", "Object"))
            {
                if (argument is CilInstructionExpression e
                    && e.Instructions.Count == 1)
                {
                    switch (e.Instructions[0].OpCode.Code)
                    {
                        case CilCode.Ldind_Ref:
                            // Load from pointer.
                            LdObj(e);
                            break;

                        case CilCode.Box:
                            // If argument is a box expression, then we can apply an optimisation; remove both
                            // box and unbox, and convert the embedded expression directly:
                            e.Arguments[0].ExpectedType = argument.ExpectedType;
                            argument.ReplaceWith(ConvertValueType(e.Arguments[0]).Remove());
                            return true;

                        default:
                            // Argument is something else. We need to unbox.
                            UnboxAny(argument);
                            break;
                    }
                }
                else
                {
                    UnboxAny(argument);
                }

                return true;
            }

            if (argument.ExpressionType is PointerTypeSignature)
            {
                ConvertValueType(argument);
                return true;
            }

            return false;
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

        private CilExpression LdObj(CilInstructionExpression argument)
        {
            var newArgument = new CilInstructionExpression(CilOpCodes.Ldobj,
                _context.ReferenceImporter.ImportType(argument.ExpectedType.ToTypeDefOrRef()))
            {
                ExpectedType = argument.ExpectedType,
                ExpressionType = argument.ExpectedType
            };
            argument.ReplaceWith(newArgument);
            
            foreach (var arg in argument.Arguments.ToArray())
                newArgument.Arguments.Add((CilExpression) arg.Remove());
            
            return newArgument;
        }

        private CilExpression Box(CilExpression argument)
        {
            var newArgument = new CilInstructionExpression(CilOpCodes.Box,
                _context.ReferenceImporter.ImportType(argument.ExpressionType.ToTypeDefOrRef()))
            {
                ExpectedType = argument.ExpectedType,
                ExpressionType = _context.TargetModule.CorLibTypeFactory.Object,
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
            if (argument.ExpectedType.FullName == argument.ExpressionType.FullName)
                return argument;
            
            var corlibType = _context.TargetModule.CorLibTypeFactory.FromName(argument.ExpectedType);
            if (corlibType == null)
            {
                var typeDef = argument.ExpectedType.Resolve();

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
                    corlibType = _context.TargetModule.TypeSystem.GetMscorlibType(underlyingType);
                }
                
                if (corlibType == null)
                    throw new RecompilerException($"Conversion from value type {argument.ExpressionType} to value type {argument.ExpectedType} is not supported yet.");
            }
            
            var opCode = SelectPrimitiveConversionOpCode(argument, corlibType.ElementType);
            var newArgument = new CilInstructionExpression(opCode)
            {
                ExpectedType = argument.ExpectedType,
                ExpressionType = argument.ExpectedType
            };
            ReplaceArgument(argument, newArgument);

            return newArgument;
        }

        private static CilOpCode SelectPrimitiveConversionOpCode(CilExpression argument, ElementType elementType)
        {
            CilOpCode code;
            switch (elementType)
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
                    throw new RecompilerException(
                        $"Conversion from value type {argument.ExpressionType} to value type {argument.ExpectedType} is not supported.");
            }

            return code;
        }

        private static void ReplaceArgument(CilExpression argument, CilInstructionExpression newArgument)
        {
            argument.ReplaceWith(newArgument);
            argument.ExpectedType = argument.ExpressionType;
            newArgument.Arguments.Add(argument);
        }
            
    }
}