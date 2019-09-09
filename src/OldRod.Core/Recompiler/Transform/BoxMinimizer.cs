using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using OldRod.Core.Ast.Cil;

namespace OldRod.Core.Recompiler.Transform
{
    public class BoxMinimizer : ChangeAwareCilAstTransform
    {
        public override string Name => "Box Minimizer";

        public override bool VisitInstructionExpression(CilInstructionExpression expression)
        {
            if (expression.Instructions.Count == 1
                && expression.Instructions[0].OpCode.Code == CilCode.Unbox_Any
                && IsBoxExpression(expression.Arguments[0], out var argument, out var boxedType)
                && expression.Instructions[0].Operand is ITypeDefOrRef type 
                && type.FullName == boxedType.FullName)
            {
                argument.ExpectedType = expression.ExpectedType;
                expression.ReplaceWith(argument.Remove());
            }
            
            return base.VisitInstructionExpression(expression);
        }

        public override bool VisitUnboxToVmExpression(CilUnboxToVmExpression expression)
        {
            var argument = expression.Expression;
            if (!expression.ExpectedType.IsValueType 
                || expression.ExpectedType.FullName == argument.ExpressionType.FullName)
            {
                argument.ExpectedType = expression.ExpectedType;
                expression.ReplaceWith(argument.Remove());

                argument.AcceptVisitor(this);
                return true;
            }

            return base.VisitUnboxToVmExpression(expression);
        }

        private bool IsBoxExpression(CilExpression expression, out CilExpression argument, out ITypeDefOrRef boxedType)
        {
            if (expression is CilInstructionExpression arg
                && arg.Instructions.Count == 1
                && arg.Instructions[0].OpCode.Code == CilCode.Box)
            {
                argument = arg.Arguments[0];
                boxedType = (ITypeDefOrRef) arg.Instructions[0].Operand;
                return true;
            }

            argument = null;
            boxedType = null;
            return false;
        }
    }
}