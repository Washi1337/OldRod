using System;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL.Pattern;

namespace OldRod.Core.Ast.IL.Transform
{
    public class StackFrameTransform : IILAstTransform
    {
        public const string Tag = "StackFrameTransform";
        
        /* The following makes an assumption on the layout of each stack frame.
         * Assuming that n is the number of arguments, and m is the number of
         * local variables, vanilla KoiVM then uses the following stack layout:
         *
         *  | Offset | Value
         *  +--------+---------------
         *  | BP - n | Argument 0
         *  : ...    : ...
         *  | BP - 3 | Argument n-2
         *  | BP - 2 | Argument n-1
         *  | BP - 1 | Return Address
         *  | BP     | Caller's BP
         *  | BP + 1 | Local 0
         *  | BP + 2 | Local 1
         *  : ...    : ...
         *  | BP + m | Local m-1
         *
         * Locals are allocated by simply increasing the SP pointer, like many
         * other calling conventions do.
         * 
         * Forks of the virtualiser plugin could deviate from this, by either
         * changing the way the stack frame is allocated, or by completely
         * revisiting the layout of a frame (i.e. adopting a different calling
         * convention).
         *
         * Newer versions of the devirtualiser could therefore benefit from a more
         * generic approach. Maybe extract an interface that represents a calling
         * convention and either detect the calling convention by other pattern
         * matching or perhaps a user-defined switch, and then use the appropriate
         * implementation of this interface?
         * 
         */
        
        private static readonly ILSequencePattern<ILStatement> AllocateLocalsPattern =
            new ILSequencePattern<ILStatement>(
                // op0 = pushr_dword(sp)
                new ILAssignmentPattern(
                    ILVariablePattern.Any(),
                    ILInstructionPattern.PushDwordReg(VMRegisters.SP)),
                // op1 = pushi_dword(locals_count)
                new ILAssignmentPattern(
                    ILVariablePattern.Any(),
                    ILInstructionPattern.PushAnyDword().Capture("push_local_count")),
                // op2 = add_dword(op0, op1)
                new ILAssignmentPattern(
                    ILVariablePattern.Any(),
                    new ILInstructionPattern(ILCode.ADD_DWORD, new ILOperandPattern(null), 
                        ILVariablePattern.Any(), ILVariablePattern.Any())),
                // sp = op2
                new ILAssignmentPattern(VMRegisters.SP, ILVariablePattern.Any())
            );

        private static readonly ILSequencePattern<ILStatement> StoreToLocalPattern =
            new ILSequencePattern<ILStatement>(
                // op0 = pushr_dword(bp)
                new ILAssignmentPattern(
                    ILVariablePattern.Any(),
                    ILInstructionPattern.PushDwordReg(VMRegisters.BP)),
                // op1 = pushi_dword(offset)
                new ILAssignmentPattern(ILVariablePattern.Any(),
                    ILInstructionPattern.PushAnyDword().Capture("push_offset")),
                // op2 = add_dword(op0, op1)
                new ILAssignmentPattern(
                    ILVariablePattern.Any(),
                    new ILInstructionPattern(ILCode.ADD_DWORD, new ILOperandPattern(null),
                        ILVariablePattern.Any(), ILVariablePattern.Any())),
                // sind_xxxx(value, op2)
                new ILExpressionStatementPattern(
                    new ILInstructionPattern(
                        new ILOpCodePattern(
                            ILCode.SIND_PTR, ILCode.SIND_BYTE,
                            ILCode.SIND_WORD, ILCode.SIND_DWORD,
                            ILCode.SIND_QWORD, ILCode.SIND_OBJECT),
                        new ILOperandPattern(null),
                        ILVariablePattern.Any(), ILVariablePattern.Any())).Capture("store")
            );
        
        /*
         * TODO: The load patterns could perhaps be generalized to a single pattern or a more clever inference algorithm,
         * because the basic structure is always the same, which is similar to e.g. x86:
         *
         * 1) Push BP onto the stack
         * 2) Push the offset of the variable onto the stack
         * 3) Add the two numbers together.
         * 3) Read the value at the new address.
         *
         * The difference however is that KoiVM uses a little trick that makes a PUSHR_DWORD BP not actually
         * push a DWORD, but a StackRef instead, which is essentially a pointer object. As a consequence, it can be used 
         * in normal pointer arithmetic using a PUSHR_OBJECT afterwards, combined with one of the ADD opcodes.
         *
         * This has a result that it saves KoiVM having to emit special VCALLs for boxing simple types like int32s
         * that can be put onto the stack directly. It introduces a bit of complexity in detecting it, so for now
         * there is a separate pattern. This might (and should) be generalised more in the future.
         */

        private static readonly ILSequencePattern<ILStatement> LoadLocalPattern =
            new ILSequencePattern<ILStatement>(
                // op0 = pushr_dword(bp)
                new ILAssignmentPattern(
                    ILVariablePattern.Any(),
                    ILInstructionPattern.PushDwordReg(VMRegisters.BP)),
                // op1 = pushi_dword(offset)
                new ILAssignmentPattern(ILVariablePattern.Any(),
                    ILInstructionPattern.PushAnyDword().Capture("push_offset")),
                // op2 = add_dword(op0, op1)
                new ILAssignmentPattern(
                    ILVariablePattern.Any(),
                    new ILInstructionPattern(ILCode.ADD_DWORD, new ILOperandPattern(null),
                        ILVariablePattern.Any(), ILVariablePattern.Any())),
                // op3 = lind_xxxx(op2)
                new ILAssignmentPattern(
                    ILVariablePattern.Any(),
                    new ILInstructionPattern(
                        new ILOpCodePattern(
                            ILCode.LIND_PTR, ILCode.LIND_BYTE,
                            ILCode.LIND_WORD, ILCode.LIND_DWORD,
                            ILCode.LIND_QWORD, ILCode.LIND_OBJECT),
                        new ILOperandPattern(null),
                        ILVariablePattern.Any()).Capture("load"))
            );
        
        private static readonly ILSequencePattern<ILStatement> LoadAsObjectPattern =
            new ILSequencePattern<ILStatement>(
                // op0 = pushr_dword(bp)
                new ILAssignmentPattern(
                    ILVariablePattern.Any(),
                    ILInstructionPattern.PushDwordReg(VMRegisters.BP)),
                // r0 = op0
                new ILAssignmentPattern(ILVariablePattern.Any(), ILVariablePattern.Any()),
                // op1 = pushr_object(r0)
                new ILAssignmentPattern(ILVariablePattern.Any(), ILInstructionPattern.PushAnyObjectReg()),
                // op2 = pushi_dword(offset)
                new ILAssignmentPattern(ILVariablePattern.Any(),
                    ILInstructionPattern.PushAnyDword().Capture("push_offset")),
                // op3 = add_qword(op1, op2)
                new ILAssignmentPattern(
                    ILVariablePattern.Any(),
                    new ILInstructionPattern(ILCode.ADD_QWORD, new ILOperandPattern(null),
                        ILVariablePattern.Any(), ILVariablePattern.Any())),
                // r0 = op3
                new ILAssignmentPattern(ILVariablePattern.Any(), ILVariablePattern.Any().Capture("final_value"))
            );
        
        public string Name => "Stack Frame Transform";

        public void ApplyTransformation(ILCompilationUnit unit, ILogger logger)
        {
            DetermineAndDeclareLocals(unit);
            ReplaceRawLocalReferences(unit, logger);
        }

        private static void DetermineAndDeclareLocals(ILCompilationUnit unit)
        {
            int localsCount = DetermineLocalCountFromPrologue(unit);

            for (int i = 0; i < localsCount; i++)
                unit.GetOrCreateVariable("local_" + i);
        }

        private static int DetermineLocalCountFromPrologue(ILCompilationUnit unit)
        {
            var entryBlock = (ILAstBlock) unit.ControlFlowGraph.Entrypoint.UserData[ILAstBlock.AstBlockProperty];
           
            var match = AllocateLocalsPattern.FindMatch(entryBlock.Statements);
            if (match.Success)
            {
                var pushLocalCount = (ILInstructionExpression) match.Captures["push_local_count"][0];
                return Convert.ToInt32(pushLocalCount.Operand);
            }
            
            return 0;
        }

        private static void ReplaceRawLocalReferences(ILCompilationUnit unit, ILogger logger)
        {
            // Find in each block the patterns for loading and/or storing local variable values,
            // and replace them with a normal variable expression or assignment statement. 
            
            foreach (var node in unit.ControlFlowGraph.Nodes)
            {
                var block = (ILAstBlock) node.UserData[ILAstBlock.AstBlockProperty];

                for (int i = 0; i < block.Statements.Count; i++)
                {
                    // TODO: Might need some extra checks after the match of a pattern.
                    //       Variables referenced in the expressions are not checked for 
                    //       equality.
                    
                    MatchResult match;
                    if ((match = StoreToLocalPattern.Match(block.Statements, i)).Success) 
                        ReplaceStoreToLocal(unit, match, logger);
                    else if ((match = LoadLocalPattern.Match(block.Statements, i)).Success)
                        ReplaceLoadToLocal(unit, match, logger);
                    else if ((match = LoadAsObjectPattern.Match(block.Statements, i)).Success)
                        ReplaceLoadAsObject(unit, match, logger);
                }
            }
        }

        private static void ReplaceStoreToLocal(ILCompilationUnit unit, MatchResult match, ILogger logger)
        {
            // Obtain variable that is referenced.
            var pushOffset = (ILInstructionExpression) match.Captures["push_offset"][0];
            int offset = unchecked((int) Convert.ToUInt32(pushOffset.Operand));
            var variable = ResolveVariable(unit, offset, logger);

            // Obtain SIND_xxxx expression.
            var statement = (ILExpressionStatement) match.Captures["store"][0];
            var sindExpr = (ILInstructionExpression) statement.Expression;
            
            // Remove value.
            var value = (ILExpression) sindExpr.Arguments[0].Remove();
            
            // Remove the original expression containing the address and unregister the
            // associated variable.
            var address = (ILVariableExpression) sindExpr.Arguments[0].Remove();
            address.Variable = null;
            
            // Replace with normal assignment.
            statement.ReplaceWith(new ILAssignmentStatement(variable, value));
        }

        private static void ReplaceLoadToLocal(ILCompilationUnit unit, MatchResult match, ILogger logger)
        {
            // Obtain variable that is referenced.
            var pushOffset = (ILInstructionExpression) match.Captures["push_offset"][0];
            int offset = unchecked((int) Convert.ToUInt32(pushOffset.Operand));
            var variable = ResolveVariable(unit, offset, logger);

            // Remove the original expression containing the address and unregister the
            // associated variable.
            var lindExpr = (ILInstructionExpression) match.Captures["load"][0];
            var address = (ILVariableExpression) lindExpr.Arguments[0].Remove();
            address.Variable = null;
            
            // Replace with normal variable expression.
            lindExpr.ReplaceWith(new ILVariableExpression(variable));
        }

        private static void ReplaceLoadAsObject(ILCompilationUnit unit, MatchResult match, ILogger logger)
        {
            // Obtain variable that is referenced.
            var pushOffset = (ILInstructionExpression) match.Captures["push_offset"][0];
            int offset = unchecked((int) Convert.ToUInt32(pushOffset.Operand));
            var variable = ResolveVariable(unit, offset, logger);

            // Obtain reference to final loaded value of the variable. 
            var finalValue = (ILVariableExpression) match.Captures["final_value"][0];
            
            // Replace with normal variable expression.
            finalValue.ReplaceWith(new ILVariableExpression(variable));
        }

        private static ILVariable ResolveVariable(ILCompilationUnit unit, int offset, ILogger logger)
        {
            string variableName;
            if (offset < -2)
            {
                int argumentIndex = unit.Signature.ParameterTokens.Count + offset + 1;
                if (argumentIndex < 0 || argumentIndex >= unit.Parameters.Count)
                {
                    logger.Warning(Tag, $"Detected reference to non-existing parameter {argumentIndex}.");
                    variableName = "arg_" + argumentIndex;
                }
                else
                {
                    return unit.Parameters[argumentIndex];
                }
            }
            else
            {
                switch (offset)
                {
                    case -1:
                        variableName = "return_address";
                        logger.Warning(Tag, "Detected reference to return address.");
                        break;
                    case 0:
                        variableName = "caller_bp";
                        logger.Warning(Tag, "Detected reference to caller base pointer (BP).");
                        break;
                    default:
                        int variableIndex = offset - 1;
                        variableName = "local_" + variableIndex;
                        break;
                }
            }

            return unit.GetOrCreateVariable(variableName);
        }
    }
}