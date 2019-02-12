using System;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Recompiler.VCallTranslation
{
    public class ECallRecompiler : IVCallRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            // TODO: check for boxing or casting.
            
            var ecall = (ECallMetadata) expression.Metadata;
            var methodSig = (MethodSignature) ecall.Method.Signature;
            
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

            var result = new CilInstructionExpression(opcode, ecall.Method);
                        
            // Emit arguments.
            for (var i = 0; i < expression.Arguments.Count - 2; i++)
            {
                var cilArgument = (CilExpression) expression.Arguments[i + 2].AcceptVisitor(context.Recompiler);

                var argumentType = methodSig.HasThis
                    ? i == 0
                        ? (ITypeDescriptor) ecall.Method.DeclaringType
                        : methodSig.Parameters[i - 1].ParameterType
                    : methodSig.Parameters[i].ParameterType;

                result.Arguments.Add(
                    cilArgument.EnsureIsType(context.ReferenceImporter.ImportType(argumentType.ToTypeDefOrRef())));
            }

            result.ExpressionType = methodSig.ReturnType;
            return result;
        }
    }
}