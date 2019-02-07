using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Transform;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Ast
{
    public class ILAstBuilder
    {
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
        
        public ILCompilationUnit BuildAst(ControlFlowGraph graph)
        {
            // TODO: maybe clone graph instead of editing directly?

            var result = BuildBasicAst(graph);

            var pipeline = new IAstTransform[]
            {
                new VariableInliner(),
            };

            foreach (var transform in pipeline)
                transform.ApplyTransformation(result);
            
            return result;
        }

        private ILCompilationUnit BuildBasicAst(ControlFlowGraph graph)
        {
            var result = new ILCompilationUnit(graph);

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
            // Introduce register variables.
            for (int i = 0; i < (int) VMRegisters.Max; i++)
            {
                var registerVar = result.GetOrCreateVariable(((VMRegisters) i).ToString());
                registerVar.VariableType = VMType.Object;
            }

            return IntroduceResultVariables(result);
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
                    var resultVar = result.GetOrCreateVariable(GetOperandVariableName(instruction, i));
                    resultVar.VariableType = dep.Type;
                    foreach (var source in dep.DataSources)
                        resultVariables[source.Offset] = resultVar;
                }
            }

            return resultVariables;
        }

        private static ILExpression BuildExpression(ILInstruction instruction, ILCompilationUnit result)
        {
            var expression = instruction.OpCode.Code == ILCode.VCALL
                ? (IArgumentsProvider) new ILVCallExpression((VCallMetadata) instruction.InferredMetadata)
                : new ILInstructionExpression(instruction);

            for (int i = 0; i < instruction.Dependencies.Count; i++)
            {
                var argument = new ILVariableExpression(
                    result.GetOrCreateVariable(GetOperandVariableName(instruction, i)));
                argument.Variable.UsedBy.Add(argument);
                expression.Arguments.Add(argument);
            }

            return (ILExpression) expression;
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
                var astBlock = new ILAstBlock();
                foreach (var instruction in ilBlock.Instructions)
                {
                    // Build expression.
                    var expression = BuildExpression(instruction, result);

                    // Add statement to result.
                    astBlock.Statements.Add(resultVariables.TryGetValue(instruction.Offset, out var resultVariable)
                        ? (ILStatement) new ILAssignmentStatement(resultVariable, expression)
                        : new ILExpressionStatement(expression));
                }

                node.UserData[ILAstBlock.AstBlockProperty] = astBlock;
            }
        }
    }
}