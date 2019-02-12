using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;

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
                    if (type.IsTypeOf("System", "Object"))
                        return Box(ExpressionType.ToTypeDefOrRef());
                    
                    if (type.IsValueType)
                    {
                        // TODO: convert value types.
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