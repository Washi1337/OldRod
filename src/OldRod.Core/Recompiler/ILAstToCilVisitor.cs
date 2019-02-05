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
using Rivers;
using Rivers.Analysis;

namespace OldRod.Core.Recompiler
{
    public class ILAstToCilVisitor : IILAstVisitor<IList<CilInstruction>>
    {
        private readonly CompilerContext _context;

        public ILAstToCilVisitor(CompilerContext context)
        {
            _context = context;
        }
        
        public IList<CilInstruction> VisitCompilationUnit(ILCompilationUnit unit)
        {
            var result = new List<CilInstruction>();

            // Register variables from the unit.
            foreach (var variable in unit.Variables)
            {
                var variableType = variable.VariableType.ToMetadataType(_context.TargetImage).ToTypeSignature();
                _context.Variables.Add(variable, new VariableSignature(variableType));
            }

            // Define block headers to use as branch targets later.
            foreach (var node in unit.ControlFlowGraph.Nodes)
                _context.BlockHeaders[node] = CilInstruction.Create(CilOpCodes.Nop);

            // Traverse all blocks in an order that keeps dominance in mind.
            // This way, the resulting code has a more natural structure rather than
            // a somewhat arbitrary order of blocks. 
            
            var dominatorInfo = new DominatorInfo(unit.ControlFlowGraph.Entrypoint);
            var dominatorTree = dominatorInfo.ToDominatorTree();
            
            var stack = new Stack<Node>();
            stack.Push(dominatorTree.Nodes[unit.ControlFlowGraph.Entrypoint.Name]);
            
            while (stack.Count > 0)
            {
                var treeNode = stack.Pop();
                var cfgNode = unit.ControlFlowGraph.Nodes[treeNode.Name];
                var block = (ILAstBlock) cfgNode.UserData[ILAstBlock.AstBlockProperty];
                
                // Add instructions of current block to result.
                result.Add(_context.BlockHeaders[cfgNode]);
                result.AddRange(block.AcceptVisitor(this));
                
                // Move on to child nodes.
                foreach (var outgoing in treeNode.OutgoingEdges)
                    stack.Push(outgoing.Target);
            }

            return result;
        }

        public IList<CilInstruction> VisitBlock(ILAstBlock block)
        {
            var result = new List<CilInstruction>();
            foreach (var statement in block.Statements)
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
                    result.AddRange(RecompilerService.GetOpCodeRecompiler(expression.OpCode.Code).Translate(_context, expression));
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
                CilInstruction.Create(CilOpCodes.Ldloc, _context.Variables[expression.Variable]),
            };
        }

        public IList<CilInstruction> VisitVCallExpression(ILVCallExpression expression)
        {
            return RecompilerService.GetVCallRecompiler(expression.Call).Translate(_context, expression);
        }
    }
}