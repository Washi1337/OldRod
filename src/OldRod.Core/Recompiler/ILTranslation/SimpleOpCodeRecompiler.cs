using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.ILTranslation
{
    public class SimpleOpCodeRecompiler : IOpCodeRecompiler
    {
        public SimpleOpCodeRecompiler(CilOpCode newOpCode, params ILCode[] opCodes)
            : this(newOpCode, opCodes.AsEnumerable())
        {
        }
        
        public SimpleOpCodeRecompiler(CilOpCode newOpCode, IEnumerable<ILCode> opCodes)
        {
            NewOpCode = newOpCode;
            OpCodes = new HashSet<ILCode>(opCodes);
        }
        
        public ISet<ILCode> OpCodes
        {
            get;
        }

        public CilOpCode NewOpCode
        {
            get;
        }

        public VMFlags AffectedFlags
        {
            get;
            set;
        }
        
        public CilExpression Translate(RecompilerContext context, ILInstructionExpression expression)
        {
            if (!OpCodes.Contains(expression.OpCode.Code))
                throw new NotSupportedException();
            
            var result = new CilInstructionExpression(NewOpCode);

            foreach (var argument in expression.Arguments)
                result.Arguments.Add((CilExpression) argument.AcceptVisitor(context.Recompiler));

            result.AffectedFlags = AffectedFlags;
            result.ShouldEmitFlagsUpdate = true;
            
            return result;
        }
    }
}