using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast;
using OldRod.Core.Disassembly.Inference;
using OldRod.Core.Recompiler.ILTranslation;
using OldRod.Core.Recompiler.VCallTranslation;

namespace OldRod.Core.Recompiler
{
    public class ILAstToCilVisitor : IILAstVisitor<IList<CilInstruction>>
    {
        private static readonly IDictionary<ILCode, IOpCodeRecompiler> OpCodeRecompilers =
            new Dictionary<ILCode, IOpCodeRecompiler>();
        
        private static readonly IDictionary<VMCalls, IVCallRecompiler> VCallRecompilers =
            new Dictionary<VMCalls, IVCallRecompiler>();

        static ILAstToCilVisitor()
        {
            var push = new PushRecompiler();
            OpCodeRecompilers[ILCode.PUSHR_BYTE] = push;
            OpCodeRecompilers[ILCode.PUSHR_WORD] = push;
            OpCodeRecompilers[ILCode.PUSHR_DWORD] = push;
            OpCodeRecompilers[ILCode.PUSHR_QWORD] = push;
            OpCodeRecompilers[ILCode.PUSHR_OBJECT] = push;
            OpCodeRecompilers[ILCode.PUSHI_DWORD] = push;
            OpCodeRecompilers[ILCode.PUSHI_QWORD] = push;
            
            var add = new AddRecompiler();
            OpCodeRecompilers[ILCode.ADD_DWORD] = add;
            OpCodeRecompilers[ILCode.ADD_QWORD] = add;
            OpCodeRecompilers[ILCode.ADD_R32] = add;
            OpCodeRecompilers[ILCode.ADD_R64] = add;

            OpCodeRecompilers[ILCode.POP] = new PopRecompiler();
            
            VCallRecompilers[VMCalls.BOX] = new BoxRecompiler();
            VCallRecompilers[VMCalls.ECALL] = new ECallRecompiler();
        }
        
        private readonly CompilerContext _context;

        public ILAstToCilVisitor(CompilerContext context)
        {
            _context = context;
        }
        
        public IList<CilInstruction> VisitCompilationUnit(ILCompilationUnit unit)
        {
            var result = new List<CilInstruction>();

            // Register variables from the unit.
            foreach (var variable in unit.GetVariables())
            {
                var variableType = variable.VariableType.ToMetadataType(_context.TargetImage).ToTypeSignature();
                _context.Variables.Add(variable, new VariableSignature(variableType));
            }

            // Traverse all statements.
            foreach (var statement in unit.Statements)
                result.AddRange(statement.AcceptVisitor(this));
            
            return result;
        }

        public IList<CilInstruction> VisitExpressionStatement(ILExpressionStatement statement)
        {
            return statement.Expression.AcceptVisitor(this);
        }

        public IList<CilInstruction> VisitAssignmentStatement(ILAssignmentStatement statement)
        {
            var result = statement.Value.AcceptVisitor(this);
            result.Add(CilInstruction.Create(CilOpCodes.Stloc, _context.Variables[statement.Variable]));
            return result;
        }

        public IList<CilInstruction> VisitInstructionExpression(ILInstructionExpression expression)
        {
            var result = new List<CilInstruction>();

            switch (expression.OpCode.FlowControl)
            {
                case ILFlowControl.Next:
                    result.AddRange(OpCodeRecompilers[expression.OpCode.Code].Translate(_context, expression));
                    break;
                case ILFlowControl.Jump:
                    result.AddRange(TranslateJumpExpression(expression));
                    break;
                case ILFlowControl.ConditionalJump:
                    result.AddRange(TranslateConditionalJumpExpression(expression));
                    break;
                case ILFlowControl.Call:
                    result.AddRange(TranslateCallExpression(expression));
                    break;
                case ILFlowControl.Return:
                    result.Add(CilInstruction.Create(CilOpCodes.Ret));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }


        private IEnumerable<CilInstruction> TranslateJumpExpression(ILInstructionExpression expression)
        {
            // TODO:
            yield break;
        }

        private IEnumerable<CilInstruction> TranslateConditionalJumpExpression(ILInstructionExpression expression)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<CilInstruction> TranslateCallExpression(ILInstructionExpression expression)
        {
            throw new NotImplementedException();
        }

        public IList<CilInstruction> VisitVariableExpression(ILVariableExpression expression)
        {
            return new[]
            {
                CilInstruction.Create(CilOpCodes.Ldloc, _context.Variables[expression.Variable]),
            };
        }

        public IList<CilInstruction> VisitVCallExpression(ILVCallExpression expression)
        {
            return VCallRecompilers[expression.Call].Translate(_context, expression);
        }
    }
}