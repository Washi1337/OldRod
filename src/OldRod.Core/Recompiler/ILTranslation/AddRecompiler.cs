using System;
using System.Collections.Generic;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.ILTranslation
{
    public class AddRecompiler : IOpCodeRecompiler
    {
        public IList<CilInstruction> Translate(CompilerContext context, ILInstructionExpression expression)
        {
            var result = new List<CilInstruction>();

            // Emit arguments.
            foreach (var argument in expression.Arguments)
                result.AddRange(argument.AcceptVisitor(context.CodeGenerator));
            
            // Emit addition instruction.
            switch (expression.OpCode.Code)
            {
                case ILCode.ADD_DWORD:
                case ILCode.ADD_QWORD:
                case ILCode.ADD_R32:
                case ILCode.ADD_R64:
                    result.Add(CilInstruction.Create(CilOpCodes.Add));
                    break;    
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;   
        }
        
    }
}