using System;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;

namespace OldRod.Core.Ast.Cil
{
    public abstract class CilExpression : CilAstNode
    {
        public ITypeDescriptor ExpressionType
        {
            get;
            set;
        }
        
        public bool IsAssignableTo(ITypeDefOrRef type)
        {
            var expressionType = ExpressionType.ToTypeDefOrRef();
            while (expressionType != null && expressionType.FullName != type.FullName)
            {
                var typeDef = (TypeDefinition) expressionType.Resolve();
                expressionType = typeDef.BaseType;
            }

            return expressionType != null;
        }

        public CilExpression EnsureIsType(ITypeDefOrRef type)
        {
            if (ExpressionType != type)
            {
                if (ExpressionType.IsValueType)
                {
                    var corlibType = type.Image.TypeSystem.GetMscorlibType(type);
                    if (corlibType != null)
                    {
                        CilOpCode opCode;
                        switch (corlibType.ElementType)
                        {
                            case ElementType.I1:
                                opCode = CilOpCodes.Conv_I1;
                                break;
                            case ElementType.U1:
                                opCode = CilOpCodes.Conv_U1;
                                break;
                            case ElementType.I2:
                                opCode = CilOpCodes.Conv_I2;
                                break;
                            case ElementType.Char:
                            case ElementType.U2:
                                opCode = CilOpCodes.Conv_U2;
                                break;
                            case ElementType.Boolean:
                            case ElementType.I4:
                                opCode = CilOpCodes.Conv_I4;
                                break;
                            case ElementType.U4:
                                opCode = CilOpCodes.Conv_U4;
                                break;
                            case ElementType.I8:
                                opCode = CilOpCodes.Conv_I8;
                                break;
                            case ElementType.U8:
                                opCode = CilOpCodes.Conv_U8;
                                break;
                            case ElementType.R4:
                                opCode = CilOpCodes.Conv_R4;
                                break;
                            case ElementType.R8:
                                opCode = CilOpCodes.Conv_R8;
                                break;
                            case ElementType.I:
                                opCode = CilOpCodes.Conv_I;
                                break;
                            case ElementType.U:
                                opCode = CilOpCodes.Conv_U;
                                break;
                            case ElementType.Object:
                                return Box(ExpressionType.ToTypeDefOrRef());
                            default:
                                return CastClass(type);
                        }

                        return new CilInstructionExpression(opCode, null, this)
                        {
                            ExpressionType = type
                        };

                    }
                }
                else if (type.IsValueType)
                {
                    return UnboxAny(type);
                }
                else if (!IsAssignableTo(type))
                {
                    return CastClass(type);
                }
            }

            return this;
        }
        
        public CilExpression CastClass(ITypeDefOrRef type)
        {
            return new CilInstructionExpression(CilOpCodes.Castclass, type, this)
            {
                ExpressionType = type
            };
        }
        
        public CilExpression Box(ITypeDefOrRef type)
        {
            return new CilInstructionExpression(CilOpCodes.Box, type, this)
            {
                ExpressionType = type
            };
        }
        
        public CilExpression UnboxAny(ITypeDefOrRef type)
        {
            return new CilInstructionExpression(CilOpCodes.Unbox_Any, type, this)
            {
                ExpressionType = type
            };
        }
    }
}