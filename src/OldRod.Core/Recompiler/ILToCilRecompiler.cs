// Project OldRod - A KoiVM devirtualisation utility.
// Copyright (C) 2019 Washi
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.ControlFlow;
using Rivers;

namespace OldRod.Core.Recompiler
{
    public class ILToCilRecompiler : IILAstVisitor<CilAstNode>
    {
        private readonly RecompilerContext _context;

        public ILToCilRecompiler(CilMethodBody methodBody, MetadataImage targetImage, IVMFunctionResolver exportResolver)
        {
            _context = new RecompilerContext(methodBody, targetImage, this, exportResolver);
        }
        
        public CilAstNode VisitCompilationUnit(ILCompilationUnit unit)
        {
            var result = new CilCompilationUnit(unit.ControlFlowGraph);

            // Convert parameters
            for (int i = 0; i < unit.Parameters.Count; i++)
            {
                var parameter = unit.Parameters[i];
                int cilIndex = i - (_context.MethodBody.Method.Signature.HasThis ? 1 : 0);
                _context.Parameters[parameter] = cilIndex == -1
                    ? _context.MethodBody.ThisParameter
                    : _context.MethodBody.Method.Signature.Parameters[cilIndex];
            }

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
                var ilBlock = (ILAstBlock) node.UserData[ILAstBlock.AstBlockProperty];
                var cilBlock = (CilAstBlock) ilBlock.AcceptVisitor(this);
                node.UserData[CilAstBlock.AstBlockProperty] = cilBlock;
            }

            return result;
        }

        public CilAstNode VisitBlock(ILAstBlock block)
        {
            var currentNode = block.GetParentNode();
            var result = (CilAstBlock) currentNode.UserData[CilAstBlock.AstBlockProperty];
            foreach (var statement in block.Statements)
                result.Statements.Add((CilStatement) statement.AcceptVisitor(this));
            return result;
        }

        public CilAstNode VisitExpressionStatement(ILExpressionStatement statement)
        {
            // Compile embedded expression.
            var node = statement.Expression.AcceptVisitor(this);
            
            // Some recompilers actually recompile the embedded expression directly to a statement (e.g. jump recompilers). 
            // If the result is just a normal expression, we need to embed it into an expression statement.
            if (node is CilExpression expression)
            {
                // Check if the expression returned anything, and therefore needs to be popped from the stack
                // as it is not used.
                if (expression.ExpressionType != null
                    && !expression.ExpressionType.IsTypeOf("System", "Void"))
                {
                    expression = new CilInstructionExpression(CilOpCodes.Pop, null, expression);
                }

                return new CilExpressionStatement(expression);
            }

            return (CilStatement) node;
        }

        public CilAstNode VisitAssignmentStatement(ILAssignmentStatement statement)
        {
            // Compile value.
            var cilExpression = (CilExpression) statement.Value.AcceptVisitor(this);
            
            // Create assignment.
            var cilVariable = _context.Variables[statement.Variable];
            var assignment = new CilInstructionExpression(CilOpCodes.Stloc, cilVariable, cilExpression.EnsureIsType(
                _context.ReferenceImporter.ImportType(cilVariable.VariableType.ToTypeDefOrRef())));
            
            return new CilExpressionStatement(assignment);
        }

        public CilAstNode VisitInstructionExpression(ILInstructionExpression expression)
        {
            // Jumps and returns are dealt with directly as they produce statements rather than expressions.
            switch (expression.OpCode.FlowControl)
            {
                case ILFlowControl.Jump:
                    return TranslateJumpExpression(expression);
                case ILFlowControl.ConditionalJump:
                    return TranslateConditionalJumpExpression(expression);
                case ILFlowControl.Return:
                    return TranslateRetExpression(expression);
                default:
                    // Forward the call to the appropriate recompiler of the opcode.
                    return RecompilerService.GetOpCodeRecompiler(expression.OpCode.Code).Translate(_context, expression);
            }
        }

        private CilStatement TranslateRetExpression(ILInstructionExpression expression)
        {
            var expr = new CilInstructionExpression(CilOpCodes.Ret);
            if (expression.Arguments.Count > 0)
            {
                var value = (CilExpression) expression.Arguments[0].AcceptVisitor(this);
                var returnType = _context.MethodBody.Method.Signature.ReturnType.ToTypeDefOrRef();
                expr.Arguments.Add(value.EnsureIsType(_context.ReferenceImporter.ImportType(returnType)));
            }

            return new CilExpressionStatement(expr);
        }

        private CilStatement TranslateJumpExpression(ILInstructionExpression expression)
        {
            var targetBlock = (CilAstBlock) expression.GetParentNode().OutgoingEdges.First()
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
            var currentNode = expression.GetParentNode();
            var trueBlock = (CilAstBlock) currentNode.OutgoingEdges
                .First(x => x.UserData.ContainsKey(ControlFlowGraph.ConditionProperty))
                .Target
                .UserData[CilAstBlock.AstBlockProperty];
            
            var falseBlock = (CilAstBlock) currentNode.OutgoingEdges
                .First(x => !x.UserData.ContainsKey(ControlFlowGraph.ConditionProperty))
                .Target
                .UserData[CilAstBlock.AstBlockProperty];

            // Create conditional jump.
            var conditionalBranch = new CilInstructionExpression(opcode, trueBlock.BlockHeader);
            conditionalBranch.Arguments.Add((CilExpression) expression.Arguments[0].AcceptVisitor(this));
            
            return new CilAstBlock
            {
                Statements =
                {
                    // Add conditional jump.
                    new CilExpressionStatement(conditionalBranch),
                    
                    // Create fall through jump:
                    // TODO: optimise away in code generator?
                    new CilExpressionStatement(new CilInstructionExpression(CilOpCodes.Br, falseBlock.BlockHeader)),
                }
            };
        }

        public CilAstNode VisitVariableExpression(ILVariableExpression expression)
        {
            if (expression.Variable is ILParameter parameter)
            {
                var cilParameter = _context.Parameters[parameter];
                return new CilInstructionExpression(CilOpCodes.Ldarg, cilParameter)
                {
                    ExpressionType = cilParameter.ParameterType
                };
            }
            else
            {
                var cilVariable = _context.Variables[expression.Variable];
                return new CilInstructionExpression(CilOpCodes.Ldloc, cilVariable)
                {
                    ExpressionType = cilVariable.VariableType
                };
            }
        }

        public CilAstNode VisitVCallExpression(ILVCallExpression expression)
        {
            return RecompilerService.GetVCallRecompiler(expression.Call).Translate(_context, expression);
        }

        public CilAstNode VisitPhiExpression(ILPhiExpression expression)
        {
            // This method should never be reached.
            
            // If it does, that means the IL AST builder did not clean up all phi nodes,
            // which could mean there is an error in one of the transformations that the
            // IL AST builder performs.
            
            throw new RecompilerException(
                "Encountered a stray phi node in the IL AST. This could mean the IL AST builder contains a "
                + "bug or is incomplete. For more details, inspect the control flow graphs generated by the IL AST "
                + "builder and each transform.");
        }
    }
}