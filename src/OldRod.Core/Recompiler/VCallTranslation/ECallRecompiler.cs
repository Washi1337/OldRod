using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast;
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

            for (var i = 0; i < expression.Arguments.Count - 2; i++)
            {
                var argument = expression.Arguments[i + 2];
                result.AddRange(argument.AcceptVisitor(context.CodeGenerator));

                if (argument.ExpressionType == VMType.Object)
                {
                    var type = context.ReferenceImporter.ImportType(methodSig.Parameters[i].ParameterType
                        .ToTypeDefOrRef());

                    if (methodSig.Parameters[i].ParameterType.IsValueType)
                        result.Add(CilInstruction.Create(CilOpCodes.Unbox_Any, type));
                    else
                        result.Add(CilInstruction.Create(CilOpCodes.Castclass, type));
                }
            }
            
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