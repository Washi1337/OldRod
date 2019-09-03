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
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL.Transform;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.DataFlow;
using OldRod.Core.Disassembly.Inference;
using OldRod.Core.Memory;

namespace OldRod.Core.Ast.IL
{
    public class ILAstBuilder
    {
        public event EventHandler<ILCompilationUnit> InitialAstBuilt;
        public event EventHandler<ILTransformEventArgs> TransformStart;
        public event EventHandler<ILTransformEventArgs> TransformEnd;
        
        private const string Tag = "AstBuilder";
        
        private readonly MetadataImage _image;
        private readonly IDictionary<string, string> _sharedVariables = new Dictionary<string, string>();
        
        public ILAstBuilder(MetadataImage image)
        {
            _image = image ?? throw new ArgumentNullException(nameof(image));
        }
        
        public ILogger Logger
        {
            get;
            set;
        } = EmptyLogger.Instance;
        
        public ILCompilationUnit BuildAst(
            ControlFlowGraph graph, 
            IFrameLayout frameLayout,
            VMConstants constants)
        {
            Logger.Debug(Tag, "Building initial IL-AST...");
            var result = BuildBasicAst(graph, frameLayout);
            OnInitialAstBuilt(result);
            
            Logger.Debug(Tag, "Applying IL-AST transforms...");
            ApplyTransformations(result, constants);
            
            return result;
        }

        private ILCompilationUnit BuildBasicAst(ControlFlowGraph graph, IFrameLayout frameLayout)
        {
            var result = new ILCompilationUnit(graph, frameLayout);

            // Introduce variables:
            Logger.Debug2(Tag, "Determining variables...");
            var resultVariables = DetermineVariables(result);

            // Build AST blocks.
            Logger.Debug2(Tag, "Building AST blocks...");
            BuildAstBlocks(result, resultVariables);

            Logger.Debug2(Tag, "Marking expressions affecting flags...");
            var marker = new FlagDataSourceMarker();
            result.AcceptVisitor(marker);

            return result;
        }

        private IDictionary<int, ILVariable> DetermineVariables(ILCompilationUnit result)
        {
            IntroduceRegisterVariables(result);
            return IntroduceResultVariables(result);
        }
       
        private static void IntroduceRegisterVariables(ILCompilationUnit result)
        {
            for (int i = 0; i < (int) VMRegisters.Max; i++)
            {
                var register = (VMRegisters) i;
                var registerVar = result.GetOrCreateVariable(register.ToString());

                registerVar.VariableType = register == VMRegisters.FL
                    ? VMType.Byte
                    : VMType.Object;
            }
        }

        private IDictionary<int, ILVariable> IntroduceResultVariables(ILCompilationUnit result)
        {
            // Determine result variables based on where the value is used by other instructions.
            // Find for each instruction the dependent instructions and assign to each of those dependent instructions
            // the same variable.
            
            var resultVariables = new Dictionary<int, ILVariable>();
            var instructions = result.ControlFlowGraph.Nodes
                .Select(x => (ILBasicBlock) x.UserData[ILBasicBlock.BasicBlockProperty])
                .SelectMany(x => x.Instructions);
            
            foreach (var instruction in instructions)
            {
                if (instruction.OpCode.Code == ILCode.CALL)
                {
                    // Calls have implicit dependencies to the parameters pushed onto the stack.
                    // We need to add them for the expression builder to work properly.
                    
                    var callAnnotation = (CallAnnotation) instruction.Annotation;
                    int parametersCount = callAnnotation.Function.FrameLayout.Parameters.Count;

                    // Create a copy of the stack and pop the first dependency (the call address) from it.
                    var stackCopy = instruction.ProgramState.Stack.Copy();
                    stackCopy.Pop(); 
                    
                    // TODO: Respect the frame layout rather than hardcoding it.
                    
                    // Pop all arguments from the stack.
                    var arguments = new SymbolicValue[parametersCount];
                    for (int i = parametersCount - 1; i >= 0; i--)
                        arguments[i] = stackCopy.Pop();

                    // Add new dependencies.
                    for (int i = 0; i < parametersCount; i++)
                        instruction.Dependencies.AddOrMerge(i + 1, arguments[i]);
                }
                
                for (int i = 0; i < instruction.Dependencies.Count; i++)
                {
                    var dep = instruction.Dependencies[i];
                    
                    // Check if the pushed value already has a result variable assigned to it.
                    var resultVar = GetExistingVariableForStackSlot(resultVariables, dep);

                    string variableName = GetOperandVariableName(instruction, i);
                    if (resultVar == null)
                    {
                        // Introduce variable for dependency.
                        resultVar = result.GetOrCreateVariable(variableName);
                        resultVar.VariableType = dep.Type;

                        // Assign this variable to all instructions that determine the value of this dependency.
                        foreach (var source in dep.DataSources)
                            resultVariables[source.Offset] = resultVar;
                    }
                    else
                    {
                        _sharedVariables[variableName] = resultVar.Name;
                    }
                    
                }
            }

            return resultVariables;
        }

        private static ILVariable GetExistingVariableForStackSlot(IReadOnlyDictionary<int, ILVariable> resultVariables, SymbolicValue dep)
        {
            foreach (var source in dep.DataSources)
            {
                if (resultVariables.TryGetValue(source.Offset, out var existingVariable))
                    return existingVariable;
            }

            return null;
        }

        private static string GetOperandVariableName(ILInstruction instruction, int operandIndex)
        {
            return $"operand_{instruction.Offset:X}_{operandIndex}";
        }

        private void BuildAstBlocks(ILCompilationUnit result, IDictionary<int, ILVariable> resultVariables)
        {
            foreach (var node in result.ControlFlowGraph.Nodes)
            {
                var ilBlock = (ILBasicBlock) node.UserData[ILBasicBlock.BasicBlockProperty];
                var astBlock = new ILAstBlock(node);
                
                foreach (var instruction in ilBlock.Instructions)
                {
                    // Build expression.
                    var expression = BuildExpression(instruction, result);

                    switch (instruction.OpCode.Code)
                    {
                        case ILCode.POP:
                        {
                            // Since we treat registers as variables, we should treat POP instructions as assignment
                            // statements instead of a normal ILExpressionStatement. This makes it easier to apply
                            // analysis and transformations (such as variable inlining) later, in the same way we do
                            // that with normal variables.
                        
                            var registerVar = result.GetOrCreateVariable(instruction.Operand.ToString());
                            var value = (ILExpression) ((IILArgumentsProvider) expression).Arguments[0].Remove();
                        
                            var assignment = new ILAssignmentStatement(registerVar, value);
                            astBlock.Statements.Add(assignment);
                            break;
                        }
                        case ILCode.CALL:
                        {
                            // CALL instructions that call non-void methods store the result in R0.
                            // TODO: Respect frame layout instead of hardcoding R0 as return value.
                            
                            var callAnnotation = (CallAnnotation) instruction.Annotation;
                            
                            var statement = callAnnotation.Function.FrameLayout.ReturnsValue
                                ? (ILStatement) new ILAssignmentStatement(
                                    result.GetOrCreateVariable(VMRegisters.R0.ToString()), expression)
                                : new ILExpressionStatement(expression);
                            
                            astBlock.Statements.Add(statement);

                            break;
                        }
                        case ILCode.RET:
                        {
                            // TODO: Respect frame layout instead of hardcoding R0 as return value.
                            var returnExpr = (IILArgumentsProvider) expression;

                            foreach (var use in expression.AcceptVisitor(VariableUsageCollector.Instance))
                                use.Variable = null;
                            
                            returnExpr.Arguments.Clear();
                            
                            if (result.FrameLayout.ReturnsValue && !instruction.ProgramState.IgnoreExitKey)
                            {
                                var registerVar = result.GetOrCreateVariable(VMRegisters.R0.ToString());
                                returnExpr.Arguments.Add(new ILVariableExpression(registerVar));
                            }

                            astBlock.Statements.Add(new ILExpressionStatement(expression));
                            break;
                        }
                        default:
                        {
                            // Build statement around expression.
                            var statement = resultVariables.TryGetValue(instruction.Offset, out var resultVariable)
                                ? (ILStatement) new ILAssignmentStatement(resultVariable, expression)
                                : new ILExpressionStatement(expression);
                        
                            astBlock.Statements.Add(statement);
                            break;
                        }
                    }
                }

                node.UserData[ILAstBlock.AstBlockProperty] = astBlock;
            }
        }

        private ILExpression BuildExpression(ILInstruction instruction, ILCompilationUnit result)
        {
            IILArgumentsProvider expression;
            switch (instruction.OpCode.Code)
            {
                case ILCode.VCALL:
                    expression = new ILVCallExpression((VCallAnnotation) instruction.Annotation);
                    break;
                
                case ILCode.PUSHR_BYTE:
                case ILCode.PUSHR_WORD:
                case ILCode.PUSHR_DWORD:
                case ILCode.PUSHR_QWORD:
                case ILCode.PUSHR_OBJECT:
                    // Since we treat registers as variables, we should interpret the operand as a variable and add it 
                    // as an argument to the expression instead of keeping it just as an operand. This makes it easier
                    // to apply analysis and transformations (such as variable inlining) later, in the same way we do
                    // that with normal variables.
                    
                    expression = new ILInstructionExpression(instruction);

                    ILVariable registerVar;
                    if (instruction.Operand is VMRegisters.FL)
                    {
                        var dataSources = instruction.ProgramState.Registers[VMRegisters.FL].DataSources
                            .Select(i => i.Offset)
                            .ToArray();
                        
                        var flagsVariable = result.GetOrCreateFlagsVariable(dataSources);
                        registerVar = flagsVariable;   
                    }
                    else
                    {
                        registerVar = result.GetOrCreateVariable(instruction.Operand.ToString());
                    }
                    
                    var varExpression = new ILVariableExpression(registerVar);
                    expression.Arguments.Add(varExpression);
                    break;
                
                default:
                    expression = new ILInstructionExpression(instruction);
                    break;
            }


            for (int i = 0; i < instruction.Dependencies.Count; i++)
            {
                ILExpression argument;
                var firstDataSource = instruction.Dependencies[i].DataSources.First();
                if (firstDataSource.Offset == InstructionProcessor.PushExceptionOffset)
                {
                    var exceptionType = (ITypeDefOrRef) firstDataSource.Operand;
                    argument = new ILExceptionExpression(exceptionType);
                }
                else
                {
                    // Get the variable containing the value of the argument and add it as an argument to the expression.
                    string variableName = GetOperandVariableName(instruction, i);
                    if (_sharedVariables.TryGetValue(variableName, out string realName))
                        variableName = realName;
                    
                    argument = new ILVariableExpression(
                        result.GetOrCreateVariable(variableName));
                }

                expression.Arguments.Add(argument);
            }

            return (ILExpression) expression;
        }

        private void ApplyTransformations(ILCompilationUnit result, VMConstants constants)
        {
            var pipeline = new IILAstTransform[]
            {
                new StackFrameTransform(),
                new SsaTransform(),
                new TransformLoop("Expression Simplification", 5, new IChangeAwareILAstTransform[]
                {
                    new VariableInliner(),
                    new PushMinimizer(), 
                    new LogicSimplifier(),
                    new FlagOperationSimplifier(constants), 
                }),
                new PhiRemovalTransform(),
            };

            foreach (var transform in pipeline)
            {
                if (transform is TransformLoop loop)
                {
                    loop.TransformStart += (sender, args) => OnTransformStart(args);
                    loop.TransformEnd += (sender, args) => OnTransformEnd(args);
                }
                
                Logger.Debug2(Tag, $"Applying {transform.Name}...");
                OnTransformStart(new ILTransformEventArgs(result, transform, 1));
                transform.ApplyTransformation(result, Logger);
                OnTransformEnd(new ILTransformEventArgs(result, transform, 1));
            }
        }

        protected virtual void OnInitialAstBuilt(ILCompilationUnit result)
        {
            InitialAstBuilt?.Invoke(this, result);
        }

        protected virtual void OnTransformStart(ILTransformEventArgs e)
        {
            TransformStart?.Invoke(this, e);
        }

        protected virtual void OnTransformEnd(ILTransformEventArgs e)
        {
            TransformEnd?.Invoke(this, e);
        }
    }
}