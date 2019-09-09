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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Ast.IL.Transform;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Recompiler.Transform;
using Rivers;

namespace OldRod.Core.Recompiler
{
    public class ILToCilRecompiler : IILAstVisitor<CilAstNode>
    {
        public const string Tag = "IL2CIL";
        
        public event EventHandler<CilCompilationUnit> InitialAstBuilt;
        public event EventHandler<CilTransformEventArgs> TransformStart;
        public event EventHandler<CilTransformEventArgs> TransformEnd;
        
        private readonly RecompilerContext _context;

        public ILToCilRecompiler(CilMethodBody methodBody, MetadataImage targetImage, IVMFunctionResolver exportResolver)
        {
            _context = new RecompilerContext(methodBody, targetImage, this, exportResolver);
        }

        public ILogger Logger
        {
            get => _context.Logger;
            set => _context.Logger = value;
        }

        public bool InferParameterTypes
        {
            get;
            set;
        }

        public CilCompilationUnit Recompile(ILCompilationUnit unit)
        {
            Logger.Debug(Tag, $"Building initial CIL AST...");
            var cilUnit = (CilCompilationUnit) unit.AcceptVisitor(this);
            OnInitialAstBuilt(cilUnit);
            
            Logger.Debug(Tag, $"Applying CIL AST transformations...");
            ApplyTransformations(cilUnit);

            foreach (var variable in _context.Variables.Values)
            {
                if (variable.AssignedBy.Count == 0 && variable.UsedBy.Count == 0)
                    cilUnit.Variables.Remove(variable);
            }
            
            return cilUnit;
        }

        private void ApplyTransformations(CilCompilationUnit cilUnit)
        {
            var transforms = new ICilAstTransform[]
            {
                new TypeInference(),
                new ArrayAccessTransform(),
                new TypeConversionInsertion(),
                new BoxMinimizer(), 
            };

            foreach (var transform in transforms)
            {
                var args = new CilTransformEventArgs(cilUnit, transform);
                Logger.Debug2(Tag, $"Applying {transform.Name}...");
                
                OnTransformStart(args);
                transform.ApplyTransformation(_context, cilUnit);
                OnTransformEnd(args);
            }
        }

        public CilAstNode VisitCompilationUnit(ILCompilationUnit unit)
        {
            var result = new CilCompilationUnit(unit.ControlFlowGraph);

            // Convert variables.
            foreach (var variable in unit.Variables)
            {
                switch (variable)
                {
                    case ILFlagsVariable _:
                    {
                        CilVariable cilVariable;
                        
                        if (result.FlagVariable == null)
                        {
                            cilVariable = new CilVariable("FL", _context.TargetImage.TypeSystem.Byte);
                            
                            result.FlagVariable = cilVariable;
                            _context.FlagVariable = cilVariable;
                            result.Variables.Add(cilVariable);
                        }

                        cilVariable = result.FlagVariable;
                        _context.Variables[variable] = cilVariable;
                        break;
                    }

                    case ILParameter parameter:
                    {
                        var methodBody = _context.MethodBody;
                        int cilIndex = parameter.ParameterIndex - (methodBody.Method.Signature.HasThis ? 1 : 0);

                        var cilParameter = new CilParameter(
                            parameter.Name,
                            cilIndex == -1
                                ? methodBody.ThisParameter.ParameterType
                                : methodBody.Method.Signature.Parameters[cilIndex].ParameterType,
                            parameter.ParameterIndex,
                            !InferParameterTypes || cilIndex == -1);

                        result.Parameters.Add(cilParameter);
                        _context.Parameters[parameter] = cilParameter;
                        break;
                    }

                    default:
                    {
                        var cilVariable = new CilVariable(variable.Name, variable.VariableType
                            .ToMetadataType(_context.TargetImage)
                            .ToTypeSignature());
                        result.Variables.Add(cilVariable);
                        _context.Variables[variable] = cilVariable;
                        break;
                    }
                }
            }

            if (result.FlagVariable == null)
            {
                var flagVariable = new CilVariable("FL", _context.TargetImage.TypeSystem.Byte);
                result.FlagVariable = flagVariable;
                _context.FlagVariable = flagVariable;
                result.Variables.Add(flagVariable);
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

            cilExpression.ExpectedType = cilVariable.VariableType;
            return new CilAssignmentStatement(cilVariable, cilExpression);
        }

        public CilAstNode VisitInstructionExpression(ILInstructionExpression expression)
        {
            // Jumps and returns are dealt with directly as they produce statements rather than expressions.

            if (expression.OpCode.Code == ILCode.LEAVE)
                return TranslateLeaveExpression(expression);
            
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
            var node = expression.GetParentNode();

            // TODO: Check EH type. Could be a finally or a filter clause, which have their own dedicated opcodes. 
            var expr = new CilInstructionExpression(
                node.SubGraphs.Count == 0
                    ? CilOpCodes.Ret
                    : CilOpCodes.Endfinally);
            
            if (expression.Arguments.Count > 0)
            {
                var value = (CilExpression) expression.Arguments[0].AcceptVisitor(this);
                value.ExpectedType = _context.MethodBody.Method.Signature.ReturnType;
                expr.Arguments.Add(value);
            }

            return new CilExpressionStatement(expr);
        }

        private CilStatement TranslateJumpExpression(ILInstructionExpression expression)
        {
            var currentNode = expression.GetParentNode();
            var targetNode = currentNode.OutgoingEdges.First().Target;
            
            var targetBlock = (CilAstBlock) targetNode.UserData[CilAstBlock.AstBlockProperty];
            bool isLeave = currentNode.SubGraphs.Except(targetNode.SubGraphs).Any();
                
            return new CilExpressionStatement(new CilInstructionExpression(
                isLeave ? CilOpCodes.Leave : CilOpCodes.Br, 
                targetBlock.BlockHeader));
        }

        private CilStatement TranslateConditionalJumpExpression(ILInstructionExpression expression)
        {
            // Choose the right opcode.
            switch (expression.OpCode.Code)
            {
                case ILCode.JZ:
                    return TranslateSimpleCondJumpExpression(expression, CilOpCodes.Brfalse);
                case ILCode.JNZ:
                    return TranslateSimpleCondJumpExpression(expression, CilOpCodes.Brtrue);
                case ILCode.SWT:
                    return TranslateSwitchExpression(expression);
                default:
                    throw new ArgumentOutOfRangeException(nameof(expression));
            }
        }

        private CilStatement TranslateSimpleCondJumpExpression(ILInstructionExpression expression, CilOpCode opCode)
        {          
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
            var conditionalBranch = new CilInstructionExpression(opCode, trueBlock.BlockHeader);
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

        private CilStatement TranslateSwitchExpression(ILInstructionExpression expression)
        {
            var currentNode = expression.GetParentNode();

            var caseBlocks = new Dictionary<int, CilAstBlock>();
            CilAstBlock defaultBlock = null; 
            foreach (var edge in currentNode.OutgoingEdges)
            {
                var targetBlock = (CilAstBlock) edge.Target.UserData[CilAstBlock.AstBlockProperty];
                
                // Check if the branch contains any conditions.
                if (edge.UserData.TryGetValue(ControlFlowGraph.ConditionProperty, out var c))
                {
                    // Collect all block targets.
                    var conditions = (IEnumerable<int>) c;
                    
                    foreach (int condition in conditions)
                    {
                        // Ignore abnormal (EH) edges.
                        if (condition != ControlFlowGraph.ExceptionConditionLabel
                            && condition != ControlFlowGraph.EndFinallyConditionLabel)
                        {
                            caseBlocks[condition] = targetBlock;
                        }
                    } 
                        
                }
                // Fall-through edge = the default case.
                else if (defaultBlock == null)
                {
                    defaultBlock = targetBlock;
                }
                // Sanity check: Multiple fall-through edges should be impossible.
                else
                {
                    throw new RecompilerException(
                        "Encountered a switch instruction that has multiple default case blocks."
                        + " This could mean the IL AST builder contains a bug or is incomplete. For more details, inspect "
                        + "the control flow graphs generated by the IL AST builder and each transform.");
                }
            }

            // Sanity check: A switch should have one default edge.
            if (defaultBlock == null)
            {
                throw new RecompilerException(
                    "Encountered a switch instruction that does not have an edge to a default case block."
                    + " This could mean the IL AST builder contains a bug or is incomplete. For more details, inspect "
                    + "the control flow graphs generated by the IL AST builder and each transform.");
            }

            // Construct labels array, and fill up the gaps.
            var caseLabels = new List<CilAstBlock>(caseBlocks.Count);
            foreach (var entry in caseBlocks.OrderBy(e => e.Key))
            {
                // Fill up the gaps with labels to the default case.
                for (int i = caseLabels.Count; i < entry.Key; i++)
                    caseLabels.Add(defaultBlock);
                
                // Add label to target case block.
                caseLabels.Add(entry.Value);
            }

            // Construct switch.
            var valueExpression = (CilExpression) expression.Arguments[1].AcceptVisitor(this);
            valueExpression.ExpectedType = _context.TargetImage.TypeSystem.Int32;
            var switchExpression = new CilInstructionExpression(
                CilOpCodes.Switch,
                caseLabels.Select(x => x.BlockHeader).ToArray(), valueExpression);

            return new CilAstBlock
            {
                Statements =
                {
                    // Add conditional jump.
                    new CilExpressionStatement(switchExpression),
                    
                    // Create fall through jump:
                    // TODO: optimise away in code generator?
                    new CilExpressionStatement(new CilInstructionExpression(CilOpCodes.Br, defaultBlock.BlockHeader)),
                }
            };   
        }

        private CilStatement TranslateLeaveExpression(ILInstructionExpression expression)
        {
            var targetBlock = (CilAstBlock) expression.GetParentNode().OutgoingEdges.First()
                .Target.UserData[CilAstBlock.AstBlockProperty];
            
            var result = new CilInstructionExpression(CilOpCodes.Leave, targetBlock.BlockHeader);
            return new CilExpressionStatement(result);
        }

        public CilAstNode VisitVariableExpression(ILVariableExpression expression)
        {
            var cilVariable = expression.Variable is ILParameter parameter
                ? _context.Parameters[parameter]
                : _context.Variables[expression.Variable];

            var result = new CilVariableExpression(cilVariable)
            {
                ExpressionType = cilVariable.VariableType,
                IsReference = expression.IsReference,
            };

            if (expression.IsReference)
                result.ExpressionType = new ByReferenceTypeSignature((TypeSignature) result.ExpressionType);
            
            return result;
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

        public CilAstNode VisitExceptionExpression(ILExceptionExpression expression)
        {
            // HACK: Leave the exception object on the stack.
            return new CilInstructionExpression(CilOpCodes.Nop)
            {
                ExpressionType = expression.ExceptionType
            }; 
        }

        protected virtual void OnInitialAstBuilt(CilCompilationUnit cilUnit)
        {
            InitialAstBuilt?.Invoke(this, cilUnit);
        }

        protected virtual void OnTransformStart(CilTransformEventArgs e)
        {
            TransformStart?.Invoke(this, e);
        }

        protected virtual void OnTransformEnd(CilTransformEventArgs e)
        {
            TransformEnd?.Invoke(this, e);
        }
    }
}