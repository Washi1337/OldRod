using System;
using System.Collections.Generic;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast;

namespace OldRod.Core.Recompiler
{
    public class ILAstToCilVisitor : IILAstVisitor<IList<CilInstruction>>
    {
        // TODO: infer variable types to prevent box/unbox instructions.
        
        private readonly MetadataImage _image;

        private readonly IDictionary<ILVariable, VariableSignature> _variables =
            new Dictionary<ILVariable, VariableSignature>();

        public ILAstToCilVisitor(MetadataImage image)
        {
            _image = image ?? throw new ArgumentNullException(nameof(image));
        }

        public ICollection<VariableSignature> Variables => _variables.Values;
        
        public IList<CilInstruction> VisitCompilationUnit(ILCompilationUnit unit)
        {
            var result = new List<CilInstruction>();

            foreach (var variable in unit.GetVariables())
                _variables.Add(variable, new VariableSignature(_image.TypeSystem.Object));
            
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
            result.Add(CilInstruction.Create(CilOpCodes.Stloc, _variables[statement.Variable]));
            return result;
        }

        public IList<CilInstruction> VisitInstructionExpression(ILInstructionExpression expression)
        {
            var result = new List<CilInstruction>();
            foreach (var argument in expression.Arguments)
                result.AddRange(argument.AcceptVisitor(this));

            switch (expression.OpCode.FlowControl)
            {
                case ILFlowControl.Next:
                    result.AddRange(TranslateSimpleExpression(expression));
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

        private IEnumerable<CilInstruction> TranslateSimpleExpression(ILInstructionExpression expression)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<CilInstruction> TranslateJumpExpression(ILInstructionExpression expression)
        {
            throw new NotImplementedException();
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
                CilInstruction.Create(CilOpCodes.Ldloc, _variables[expression.Variable]),
            };
        }
    }
}