using AsmResolver.PE.DotNet.Cil;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.VCall
{
    public class CkOverflowRecompiler : IVCallRecompiler
    {
        public const string Tag = "CkOverflow";
        
        /// <inheritdoc />
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            // CKOVERFLOW is currently not supported. This is in most cases not a big issue as the extra properties of
            // the overflow checked instructions are not really used all that often in real world applications.
            // We therefore just emit a NOP for now.
            
            var method = context.MethodBody.Owner;

            string displayName = method.MetadataToken != 0 
                ? method.MetadataToken.ToInt32().ToString("X8") 
                : method.Name;

            context.Logger.Warning(Tag,
                $"Virtualized method {displayName} contains overflow checks which are not supported by OldRod. Resulting code might be inaccurate.");

            return new CilInstructionExpression(CilOpCodes.Nop);
        }
    }
}