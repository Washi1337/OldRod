using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.ILTranslation
{
    public class NorRecompiler : SimpleOpCodeRecompiler
    {
        public NorRecompiler() 
            : base(CilOpCodes.Or, ILCode.NOR_DWORD, ILCode.NOR_QWORD)
        {
        }
        
        public override CilExpression Translate(RecompilerContext context, ILInstructionExpression expression)
        {
            var result = base.Translate(context, expression);

            return new CilInstructionExpression(CilOpCodes.Not, null, result)
            {
                ExpressionType = result.ExpressionType,
//                AffectedFlags = VMFlags.ZERO | VMFlags.SIGN,
//                ShouldEmitFlagsUpdate = true
            };
        }
        
    }
}