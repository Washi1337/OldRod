using System;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL.Pattern;

namespace OldRod.Core.Ast.IL.Transform
{
    public class StackFrameTransform : IAstTransform
    {
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
        
        public string Name => "Stack frame transform";

        public void ApplyTransformation(ILCompilationUnit unit)
        {
            DetermineAndDeclareLocals(unit);
            ReplaceRawLocalReferences(unit);
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

        private static void ReplaceRawLocalReferences(ILCompilationUnit unit)
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
                        ReplaceStoreToLocal(unit, match);
                    else if ((match = LoadLocalPattern.Match(block.Statements, i)).Success)
                        ReplaceLoadToLocal(unit, match);
                }
            }
        }

        private static void ReplaceStoreToLocal(ILCompilationUnit unit, MatchResult match)
        {
            // Obtain variable that is referenced.
            var pushOffset = (ILInstructionExpression) match.Captures["push_offset"][0];
            int offset = Convert.ToInt32(pushOffset.Operand);
            var variable = unit.GetOrCreateVariable("local_" + offset);

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

        private static void ReplaceLoadToLocal(ILCompilationUnit unit, MatchResult match)
        {
            // Obtain variable that is referenced.
            var pushOffset = (ILInstructionExpression) match.Captures["push_offset"][0];
            int offset = Convert.ToInt32(pushOffset.Operand);
            var variable = unit.GetOrCreateVariable("local_" + offset);

            // Remove the original expression containing the address and unregister the
            // associated variable.
            var lindExpr = (ILInstructionExpression) match.Captures["load"][0];
            var address = (ILVariableExpression) lindExpr.Arguments[0].Remove();
            address.Variable = null;
            
            // Replace with normal variable expression.
            lindExpr.ReplaceWith(new ILVariableExpression(variable));
        }
    }
}