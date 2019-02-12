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

        public CilExpression EnsureIsType(ITypeDefOrRef type)
        {
            // TODO: keep base types into account.
            
            if (ExpressionType != type)
            {
                if (ExpressionType.IsValueType)
                {
                    if (!type.IsValueType)
                        return Box(type);
                }
                else if (type.IsValueType)
                {
                    return this; // TODO: convert value types.
                }
                else
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