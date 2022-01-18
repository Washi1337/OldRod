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
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using OldRod.Core.Ast.Cil;

namespace OldRod.Core.Recompiler.Transform
{
    public class ArrayAccessTransform : ICilAstTransform, ICilAstVisitor
    {
        private RecompilerContext _context;

        public string Name => "Array Access Transform";

        public void ApplyTransformation(RecompilerContext context, CilCompilationUnit unit)
        {
            _context = context;
            VisitCompilationUnit(unit);
        }

        public void VisitCompilationUnit(CilCompilationUnit unit)
        {
            foreach (var child in unit.GetChildren())
                child.AcceptVisitor(this);
        }

        public void VisitBlock(CilAstBlock block)
        {
            foreach (var statement in block.Statements)
                statement.AcceptVisitor(this);
        }

        public void VisitExpressionStatement(CilExpressionStatement statement)
        {
            statement.Expression.AcceptVisitor(this);
        }

        public void VisitAssignmentStatement(CilAssignmentStatement statement)
        {
            statement.Value.AcceptVisitor(this);
        }

        public void VisitInstructionExpression(CilInstructionExpression expression)
        {
            if (expression.Instructions.Count == 1
                && IsArrayMemberAccess(expression.Instructions[0], out var method))
            {
                var arrayExpr = expression.Arguments[0];

                if (arrayExpr.ExpressionType is SzArrayTypeSignature arrayType)
                {
                    if (IsArrayGetLength(method))
                        ReplaceWithLdlen(expression, arrayType);
                    else if (IsArrayGetValue(method))
                        ReplaceWithLdelem(expression, arrayType);
                    else if (IsArraySetValue(method))
                        ReplaceWithStelem(expression, arrayType);
                    else if (IsArrayAddress(method))
                        ReplaceWithLdelema(expression, arrayType);
                }
            }

            foreach (var argument in expression.Arguments.ToArray())
                argument.AcceptVisitor(this);
        }

        public void VisitUnboxToVmExpression(CilUnboxToVmExpression expression)
        {
            expression.Expression.AcceptVisitor(this);
        }

        public void VisitVariableExpression(CilVariableExpression expression)
        {
        }

        private static bool IsArrayMemberAccess(CilInstruction instruction, out IMethodDefOrRef memberRef)
        {
            if (instruction.OpCode.Code == CilCode.Call
                && instruction.Operand is IMethodDefOrRef m
                && (m.DeclaringType.IsTypeOf("System", "Array") 
                    || (m.DeclaringType is TypeSpecification ts 
                        && ts.Signature is SzArrayTypeSignature)))
            {
                memberRef = m;
                return true;
            }
            
            memberRef = null;
            return false;
        }

        private static bool IsArrayGetLength(IMethodDefOrRef memberRef)
        {
            return memberRef.Name == "get_Length"
                   && memberRef.Signature is MethodSignature methodSig
                   && methodSig.ParameterTypes.Count == 0
                   && methodSig.HasThis
                   && methodSig.ReturnType.IsTypeOf("System", "Int32");
        }

        private static bool IsArrayGetValue(IMethodDefOrRef memberRef)
        {
            return memberRef.Name == "GetValue"
                   && memberRef.Signature is MethodSignature methodSig
                   && methodSig.ParameterTypes.Count == 1
                   && methodSig.HasThis
                   && methodSig.ReturnType.IsTypeOf("System", "Object")
                   && methodSig.ParameterTypes[0].IsTypeOf("System", "Int32");
        }

        private static bool IsArraySetValue(IMethodDefOrRef memberRef)
        {
            return memberRef.Name == "SetValue"
                   && memberRef.Signature is MethodSignature methodSig
                   && methodSig.ParameterTypes.Count == 2
                   && methodSig.HasThis
                   && methodSig.ReturnType.IsTypeOf("System", "Void")
                   && methodSig.ParameterTypes[0].IsTypeOf("System", "Object")
                   && methodSig.ParameterTypes[1].IsTypeOf("System", "Int32");
        }
        
        private static bool IsArrayAddress(IMethodDefOrRef memberRef)
        {
            return memberRef.Name == "Address"
                   && memberRef.Signature is MethodSignature methodSig
                   && methodSig.ParameterTypes.Count == 1
                   && methodSig.HasThis
                   && methodSig.ReturnType is ByReferenceTypeSignature
                   && methodSig.ParameterTypes[0].IsTypeOf("System", "Int32");
        }

        private void ReplaceWithLdlen(CilInstructionExpression expression, SzArrayTypeSignature arrayType)
        {
            var arrayExpr = expression.Arguments[0];
            arrayExpr.ExpectedType = arrayType;

            var arrayLoadExpr = new CilInstructionExpression(CilOpCodes.Ldlen, null,
                (CilExpression) arrayExpr.Remove())
            {
                ExpressionType = arrayType.BaseType
            };

            expression.ReplaceWith(arrayLoadExpr);
        }
        
        private void ReplaceWithLdelem(CilInstructionExpression expression, SzArrayTypeSignature arrayType)
        {
            var arrayExpr = expression.Arguments[0];
            var indexExpr = expression.Arguments[1];

            arrayExpr.ExpectedType = arrayType;
            var elementTypeRef = _context.ReferenceImporter
                .ImportType(arrayType.BaseType.ToTypeDefOrRef());

            // Select appropriate opcode.
            CilOpCode opCode;
            object operand = null;
            switch (arrayType.BaseType.ElementType)
            {
                case ElementType.I1:
                    opCode = CilOpCodes.Ldelem_I1;
                    break;
                case ElementType.U1:
                    opCode = CilOpCodes.Ldelem_U1;
                    break;
                case ElementType.I2:
                    opCode = CilOpCodes.Ldelem_I2;
                    break;
                case ElementType.Char:
                case ElementType.U2:
                    opCode = CilOpCodes.Ldelem_U2;
                    break;
                case ElementType.Boolean:
                case ElementType.I4:
                    opCode = CilOpCodes.Ldelem_I4;
                    break;
                case ElementType.U4:
                    opCode = CilOpCodes.Ldelem_U4;
                    break;
                case ElementType.I8:
                case ElementType.U8:
                    opCode = CilOpCodes.Ldelem_I8;
                    break;
                case ElementType.R4:
                    opCode = CilOpCodes.Ldelem_R4;
                    break;
                case ElementType.R8:
                    opCode = CilOpCodes.Ldelem_R8;
                    break;
                case ElementType.I:
                case ElementType.U:
                    opCode = CilOpCodes.Ldelem_I;
                    break;
                case ElementType.ValueType:
                    opCode = CilOpCodes.Ldelem;
                    operand = elementTypeRef;
                    break;
                default:
                    opCode = CilOpCodes.Ldelem_Ref;
                    break;
            }
            
            // Create the ldelem expression
            var arrayLoadExpr = new CilInstructionExpression(opCode, operand,
                (CilExpression) arrayExpr.Remove(),
                (CilExpression) indexExpr.Remove())
            {
                ExpressionType = arrayType.BaseType
            };

            if (arrayType.BaseType.IsValueType)
            {
                // Array.GetValue boxes value typed values.
                arrayLoadExpr = new CilInstructionExpression(CilOpCodes.Box, elementTypeRef, arrayLoadExpr)
                {
                    ExpectedType = expression.ExpectedType,
                    ExpressionType = expression.ExpressionType
                };
            }

            expression.ReplaceWith(arrayLoadExpr);
        }

        private void ReplaceWithStelem(CilInstructionExpression expression, SzArrayTypeSignature arrayType)
        {
            var arrayExpr = expression.Arguments[0];
            var valueExpr = expression.Arguments[1];
            var indexExpr = expression.Arguments[2];

            arrayExpr.ExpectedType = arrayType;

            // Select appropriate opcode.
            CilOpCode opCode;
            object operand = null;
            switch (arrayType.BaseType.ElementType)
            {
                case ElementType.I1:
                case ElementType.U1:
                    opCode = CilOpCodes.Stelem_I1;
                    break;
                case ElementType.Char:
                case ElementType.I2:
                case ElementType.U2:
                    opCode = CilOpCodes.Stelem_I2;
                    break;
                case ElementType.Boolean:
                case ElementType.I4:
                case ElementType.U4:
                    opCode = CilOpCodes.Stelem_I4;
                    break;
                case ElementType.I8:
                case ElementType.U8:
                    opCode = CilOpCodes.Stelem_I8;
                    break;
                case ElementType.R4:
                    opCode = CilOpCodes.Stelem_R4;
                    break;
                case ElementType.R8:
                    opCode = CilOpCodes.Stelem_R8;
                    break;
                case ElementType.I:
                case ElementType.U:
                    opCode = CilOpCodes.Stelem_I;
                    break; 
                case ElementType.ValueType:
                    opCode = CilOpCodes.Stelem;
                    operand = _context.ReferenceImporter
                        .ImportType(arrayType.BaseType.ToTypeDefOrRef());
                    break;
                default:
                    opCode = CilOpCodes.Stelem_Ref;
                    break;
            }
            
            valueExpr.ExpectedType = arrayType.BaseType;

            var arrayStoreExpr = new CilInstructionExpression(opCode, operand,
                (CilExpression) arrayExpr.Remove(),
                (CilExpression) indexExpr.Remove(),
                (CilExpression) valueExpr.Remove());

            expression.ReplaceWith(arrayStoreExpr);
        }

        private void ReplaceWithLdelema(CilInstructionExpression expression, SzArrayTypeSignature arrayType)
        {
            var arrayExpr = expression.Arguments[0];
            var indexExpr = expression.Arguments[1];

            arrayExpr.ExpectedType = arrayType;
            var elementTypeRef = _context.ReferenceImporter
                .ImportType(arrayType.BaseType.ToTypeDefOrRef());
            
            var arrayLoadExpr = new CilInstructionExpression(CilOpCodes.Ldelema, elementTypeRef,
                (CilExpression) arrayExpr.Remove(),
                (CilExpression) indexExpr.Remove())
            {
                ExpressionType = new ByReferenceTypeSignature(arrayType.BaseType)
            };
            
            expression.ReplaceWith(arrayLoadExpr);
        }
    }
}