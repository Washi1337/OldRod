using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL.Transform;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Ast.IL
{
    public class ILAstBuilder
    {
        public event EventHandler<IAstTransform> TransformStart;
        public event EventHandler<IAstTransform> TransformEnd;
        
        private const string Tag = "AstBuilder";
        
        private readonly MetadataImage _image;

        public ILAstBuilder(MetadataImage image)
        {
            _image = image ?? throw new ArgumentNullException(nameof(image));
        }
        
        public ILogger Logger
        {
            get;
            set;
        } = EmptyLogger.Instance;
        
        public ILCompilationUnit BuildAst(VMFunctionSignature signature, ControlFlowGraph graph)
        {
            var result = BuildBasicAst(signature, graph);
            ApplyTransformations(result);
            return result;
        }

        private ILCompilationUnit BuildBasicAst(VMFunctionSignature signature, ControlFlowGraph graph)
        {
            var result = new ILCompilationUnit(signature, graph);

            // Introduce variables:
            Logger.Debug(Tag, "Determining variables...");
            var resultVariables = DetermineVariables(result);

            // Build AST blocks.
            Logger.Debug(Tag, "Building AST blocks...");
            BuildAstBlocks(result, resultVariables);

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
                for (int i = 0; i < instruction.Dependencies.Count; i++)
                {
                    var dep = instruction.Dependencies[i];
                    
                    // Introduce variable for dependency.
                    var resultVar = result.GetOrCreateVariable(GetOperandVariableName(instruction, i));
                    resultVar.VariableType = dep.Type;
                    
                    // Assign this variable to all instructions that determine the value of this dependency.
                    foreach (var source in dep.DataSources)
                        resultVariables[source.Offset] = resultVar;
                }
            }

            return resultVariables;
        }

        private static string GetOperandVariableName(ILInstruction instruction, int operandIndex)
        {
            return $"operand_{instruction.Offset:X}_{operandIndex}";
        }

        private static void BuildAstBlocks(ILCompilationUnit result, IDictionary<int, ILVariable> resultVariables)
        {
            foreach (var node in result.ControlFlowGraph.Nodes)
            {
                var ilBlock = (ILBasicBlock) node.UserData[ILBasicBlock.BasicBlockProperty];
                var astBlock = new ILAstBlock(node);
                foreach (var instruction in ilBlock.Instructions)
                {
                    // Build expression.
                    var expression = BuildExpression(instruction, result);

                    if (instruction.OpCode.Code == ILCode.POP)
                    {
                        // Since we treat registers as variables, we should treat POP instructions as assignment
                        // statements instead of a normal ILExpressionStatement. This makes it easier to apply
                        // analysis and transformations (such as variable inlining) later, in the same way we do
                        // that with normal variables.
                        
                        var registerVar = result.GetOrCreateVariable(instruction.Operand.ToString());
                        var value = (ILExpression) ((IILArgumentsProvider) expression).Arguments[0].Remove();
                        
                        var assignment = new ILAssignmentStatement(registerVar, value);
                        astBlock.Statements.Add(assignment);
                    }
                    else
                    {
                        // Build statement around expression.
                        var statement = resultVariables.TryGetValue(instruction.Offset, out var resultVariable)
                            ? (ILStatement) new ILAssignmentStatement(resultVariable, expression)
                            : new ILExpressionStatement(expression);

                        astBlock.Statements.Add(statement);
                    }
                }

                node.UserData[ILAstBlock.AstBlockProperty] = astBlock;
            }
        }

        private static ILExpression BuildExpression(ILInstruction instruction, ILCompilationUnit result)
        {
            IILArgumentsProvider expression;
            switch (instruction.OpCode.Code)
            {
                case ILCode.VCALL:
                    expression = new ILVCallExpression((VCallMetadata) instruction.InferredMetadata);
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
                    var registerVar = result.GetOrCreateVariable(instruction.Operand.ToString());
                    var varExpression = new ILVariableExpression(registerVar);
                    expression.Arguments.Add(varExpression);
                    break;
                
                default:
                    expression = new ILInstructionExpression(instruction);
                    break;
            }


            for (int i = 0; i < instruction.Dependencies.Count; i++)
            {
                // Get the variable containing the value of the argument and add it as an argument to the expression.
                var argument = new ILVariableExpression(
                    result.GetOrCreateVariable(GetOperandVariableName(instruction, i)));
                expression.Arguments.Add(argument);
            }

            return (ILExpression) expression;
        }

        private void ApplyTransformations(ILCompilationUnit result)
        {
            var pipeline = new IAstTransform[]
            {
                new StackFrameTransform(), 
                new SsaTransform(), 
                new VariableInliner(),
                new PhiRemovalTransform(), 
            };

            foreach (var transform in pipeline)
            {
                Logger.Debug(Tag, $"Applying {transform.Name}...");
                OnTransformStart(transform);
                transform.ApplyTransformation(result, Logger);
                OnTransformEnd(transform);
            }
        }

        protected virtual void OnTransformStart(IAstTransform e)
        {
            TransformStart?.Invoke(this, e);
        }

        protected virtual void OnTransformEnd(IAstTransform e)
        {
            TransformEnd?.Invoke(this, e);
        }
    }
}