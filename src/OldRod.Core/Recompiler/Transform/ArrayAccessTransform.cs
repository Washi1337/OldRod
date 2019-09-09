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
                && m.DeclaringType.IsTypeOf("System", "Array"))
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
                   && methodSig.Parameters.Count == 0
                   && methodSig.HasThis
                   && methodSig.ReturnType.IsTypeOf("System", "Int32");
        }

        private static bool IsArrayGetValue(IMethodDefOrRef memberRef)
        {
            return memberRef.Name == "GetValue"
                   && memberRef.Signature is MethodSignature methodSig
                   && methodSig.Parameters.Count == 1
                   && methodSig.HasThis
                   && methodSig.ReturnType.IsTypeOf("System", "Object")
                   && methodSig.Parameters[0].ParameterType.IsTypeOf("System", "Int32");
        }

        private static bool IsArraySetValue(IMethodDefOrRef memberRef)
        {
            return memberRef.Name == "SetValue"
                   && memberRef.Signature is MethodSignature methodSig
                   && methodSig.Parameters.Count == 2
                   && methodSig.HasThis
                   && methodSig.ReturnType.IsTypeOf("System", "Void")
                   && methodSig.Parameters[0].ParameterType.IsTypeOf("System", "Object")
                   && methodSig.Parameters[1].ParameterType.IsTypeOf("System", "Int32");
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
                    operand = _context.ReferenceImporter
                        .ImportType(arrayType.BaseType.ToTypeDefOrRef());
                    break;
                default:
                    opCode = CilOpCodes.Ldelem_Ref;
                    break;
            }
            
            var arrayLoadExpr = new CilInstructionExpression(opCode, operand,
                (CilExpression) arrayExpr.Remove(),
                (CilExpression) indexExpr.Remove())
            {
                ExpressionType = arrayType.BaseType
            };

            expression.ReplaceWith(arrayLoadExpr);
        }

        private void ReplaceWithStelem(CilInstructionExpression expression, SzArrayTypeSignature arrayType)
        {
            var arrayExpr = expression.Arguments[0];
            var valueExpr = expression.Arguments[1];
            var indexExpr = expression.Arguments[2];

            arrayExpr.ExpectedType = arrayType;

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

    }
}