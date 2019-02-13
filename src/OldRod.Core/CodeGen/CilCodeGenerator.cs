using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Signatures;
using OldRod.Core.Ast.Cil;
using Rivers;
using Rivers.Analysis;

namespace OldRod.Core.CodeGen
{
    public class CilCodeGenerator : ICilAstVisitor<IList<CilInstruction>>
    {
        private readonly CodeGenerationContext _context;

        public CilCodeGenerator(CodeGenerationContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        
        public IList<CilInstruction> VisitCompilationUnit(CilCompilationUnit unit)
        {
            var result = new List<CilInstruction>();

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
                var block = (CilAstBlock) cfgNode.UserData[CilAstBlock.AstBlockProperty];
                
                // Add instructions of current block to result.
                result.AddRange(block.AcceptVisitor(this));
                
                // Move on to child nodes.
                var directChildren = new HashSet<Node>();
                foreach (var outgoing in treeNode.OutgoingEdges)
                {
                    var outgoingTarget = outgoing.Target;
                    if (cfgNode.GetSuccessors().All(x => x.Name != outgoingTarget.Name))
                        stack.Push(outgoingTarget);
                    else
                        directChildren.Add(outgoingTarget);
                }

                foreach (var child in directChildren)
                    stack.Push(child);
            }

            return result;
        }

        public IList<CilInstruction> VisitBlock(CilAstBlock block)
        {
            var result = new List<CilInstruction>();
            result.Add(block.BlockHeader);
            foreach (var statement in block.Statements)
                result.AddRange(statement.AcceptVisitor(this));
            return result;
        }

        public IList<CilInstruction> VisitExpressionStatement(CilExpressionStatement statement)
        {
            return statement.Expression.AcceptVisitor(this);
        }

        public IList<CilInstruction> VisitInstructionExpression(CilInstructionExpression expression)
        {
            var result = new List<CilInstruction>();

            if (expression.ShouldEmitFlagsUpdate)
            {
                result.AddRange(_context.BuildBinaryExpression(
                    expression.Arguments[0].AcceptVisitor(this),
                    expression.Arguments[1].AcceptVisitor(this),
                    new[] {CilInstruction.Create(CilOpCodes.Sub)},
                    _context.Constants.GetFlagMask(expression.AffectedFlags)));
            }
            else
            {
                foreach (var argument in expression.Arguments)
                    result.AddRange(argument.AcceptVisitor(this));
                result.Add(new CilInstruction(0, expression.OpCode, expression.Operand));
            }

            if (expression.Operand is VariableSignature variable && !_context.Variables.Contains(variable))
                _context.Variables.Add(variable);
            

            return result;
        }
    }
}