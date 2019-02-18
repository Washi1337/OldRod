using System;
using System.Linq;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.ControlFlow;
using Rivers;

namespace OldRod.Core.Recompiler
{
    public class ILToCilRecompiler : IILAstVisitor<CilAstNode>
    {
        private readonly RecompilerContext _context;
        private Node _currentNode;
        
        public ILToCilRecompiler(MetadataImage targetImage)
        {
            _context = new RecompilerContext(targetImage, this);
        }
        
        public CilAstNode VisitCompilationUnit(ILCompilationUnit unit)
        {
            var result = new CilCompilationUnit(unit.ControlFlowGraph);

            // Convert variables.
            foreach (var variable in unit.Variables)
            {
                var cilVariable = new VariableSignature(variable.VariableType
                    .ToMetadataType(_context.TargetImage)
                    .ToTypeSignature());

                _context.Variables[variable] = cilVariable;
                result.Variables.Add(cilVariable);

                if (variable.Name == "FL")
                {
                    result.FlagVariable = cilVariable;
                    _context.FlagVariable = cilVariable;
                }
            }
            
            // Create all Cil blocks.
            foreach (var node in result.ControlFlowGraph.Nodes)
                node.UserData[CilAstBlock.AstBlockProperty] = new CilAstBlock();

            // Convert all IL blocks.
            foreach (var node in result.ControlFlowGraph.Nodes)
            {
                _currentNode = node;
                var ilBlock = (ILAstBlock) node.UserData[ILAstBlock.AstBlockProperty];
                var cilBlock = (CilAstBlock) ilBlock.AcceptVisitor(this);
                node.UserData[CilAstBlock.AstBlockProperty] = cilBlock;
            }

            return result;
        }

        public CilAstNode VisitBlock(ILAstBlock block)
        {
            var result = (CilAstBlock) _currentNode.UserData[CilAstBlock.AstBlockProperty];
            foreach (var statement in block.Statements)
                result.Statements.Add((CilStatement) statement.AcceptVisitor(this));
            return result;
        }

        public CilAstNode VisitExpressionStatement(ILExpressionStatement statement)
        {
            var node = statement.Expression.AcceptVisitor(this);
            if (node is CilExpression expression)
                return new CilExpressionStatement(expression);
            return (CilStatement) node;
        }

        public CilAstNode VisitAssignmentStatement(ILAssignmentStatement statement)
        {
            var cilExpression = (CilExpression) statement.Value.AcceptVisitor(this);
            var cilVariable = _context.Variables[statement.Variable];

            var assignment = new CilInstructionExpression(CilOpCodes.Stloc, cilVariable, cilExpression.EnsureIsType(
                _context.ReferenceImporter.ImportType(cilVariable.VariableType.ToTypeDefOrRef())));
            
            return new CilExpressionStatement(assignment);
        }

        public CilAstNode VisitInstructionExpression(ILInstructionExpression expression)
        {
            switch (expression.OpCode.FlowControl)
            {
                case ILFlowControl.Next:
                    return RecompilerService.GetOpCodeRecompiler(expression.OpCode.Code).Translate(_context, expression);
                case ILFlowControl.Jump:
                    return TranslateJumpExpression(expression);
                case ILFlowControl.ConditionalJump:
                    return TranslateConditionalJumpExpression(expression);
                case ILFlowControl.Call:
                    return TranslateCallExpression(expression);
                case ILFlowControl.Return:
                    return new CilExpressionStatement(new CilInstructionExpression(CilOpCodes.Ret));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private CilStatement TranslateJumpExpression(ILInstructionExpression expression)
        {
            var targetBlock = (CilAstBlock) _currentNode.OutgoingEdges.First()
                .Target.UserData[CilAstBlock.AstBlockProperty];

            return new CilExpressionStatement(new CilInstructionExpression(CilOpCodes.Br, targetBlock.BlockHeader));
        }

        private CilStatement TranslateConditionalJumpExpression(ILInstructionExpression expression)
        {
            // Choose the right opcode.
            CilOpCode opcode;
            switch (expression.OpCode.Code)
            {
                case ILCode.JZ:
                    opcode = CilOpCodes.Brfalse;
                    break;
                case ILCode.JNZ:
                    opcode = CilOpCodes.Brtrue;
                    break;
                default:
                    throw new NotSupportedException();
            }

            // Figure out target blocks.
            var trueBlock = (CilAstBlock) _currentNode.OutgoingEdges
                .First(x => x.UserData.ContainsKey(ControlFlowGraph.ConditionProperty))
                .Target
                .UserData[CilAstBlock.AstBlockProperty];
            
            var falseBlock = (CilAstBlock) _currentNode.OutgoingEdges
                .First(x => !x.UserData.ContainsKey(ControlFlowGraph.ConditionProperty))
                .Target
                .UserData[CilAstBlock.AstBlockProperty];

            return new CilAstBlock
            {
                Statements =
                {
                    // Create conditional jump:
                    new CilExpressionStatement(new CilInstructionExpression(opcode, trueBlock.BlockHeader, 
                        new CilInstructionExpression(CilOpCodes.Ldloc, _context.FlagVariable))),
                    
                    // Create fall through jump:
                    // TODO: optimise away in code generator?
                    new CilExpressionStatement(new CilInstructionExpression(CilOpCodes.Br, falseBlock.BlockHeader)),
                }
            };
        }

        private CilExpression TranslateCallExpression(ILInstructionExpression expression)
        {
            throw new NotImplementedException();
        }

        public CilAstNode VisitVariableExpression(ILVariableExpression expression)
        {
            var variable = _context.Variables[expression.Variable];
            return new CilInstructionExpression(CilOpCodes.Ldloc, variable)
            {
                ExpressionType = variable.VariableType
            };
        }

        public CilAstNode VisitVCallExpression(ILVCallExpression expression)
        {
            return RecompilerService.GetVCallRecompiler(expression.Call).Translate(_context, expression);
        }

        public CilAstNode VisitPhiExpression(ILPhiExpression expression)
        {
            throw new NotImplementedException();
        }
    }
}