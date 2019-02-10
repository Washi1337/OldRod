using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Recompiler.VCallTranslation
{
    public class ECallRecompiler : IVCallRecompiler
    {
        public IList<CilInstruction> Translate(CompilerContext context, ILVCallExpression expression)
        {
            var result = new List<CilInstruction>();
            var ecall = (ECallMetadata) expression.Metadata;
            var methodSig = (MethodSignature) ecall.Method.Signature;

            // Emit arguments.
            for (var i = 0; i < expression.Arguments.Count - 2; i++)
            {
                var argument = expression.Arguments[i + 2];
                result.AddRange(argument.AcceptVisitor(context.CodeGenerator));

                // Check if any casting or unboxing has to be done.
                if (argument.ExpressionType == VMType.Object)
                {
                    var type = context.ReferenceImporter.ImportType(
                        methodSig.Parameters[i].ParameterType.ToTypeDefOrRef());

                    result.Add(CilInstruction.Create(methodSig.Parameters[i].ParameterType.IsValueType
                            ? CilOpCodes.Unbox_Any
                            : CilOpCodes.Castclass,
                        type));
                }
            }
            
            // Emit calling instruction.
            CilOpCode opcode;
            switch (ecall.OpCode)
            {
                case VMECallOpCode.ECALL_CALL:
                    opcode = CilOpCodes.Call;
                    break;
                case VMECallOpCode.ECALL_CALLVIRT:
                    opcode = CilOpCodes.Callvirt;
                    break;
                case VMECallOpCode.ECALL_NEWOBJ:
                    opcode = CilOpCodes.Newobj;
                    break;
                case VMECallOpCode.ECALL_CALLVIRT_CONSTRAINED:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }

            result.Add(CilInstruction.Create(opcode, ecall.Method));
            
            return result;
        }
    }
}