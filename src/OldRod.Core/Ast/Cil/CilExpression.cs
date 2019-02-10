using AsmResolver.Net;

namespace OldRod.Core.Ast.Cil
{
    public abstract class CilExpression : CilAstNode
    {
        public ITypeDescriptor ExpressionType
        {
            get;
            set;
        }
    }
}