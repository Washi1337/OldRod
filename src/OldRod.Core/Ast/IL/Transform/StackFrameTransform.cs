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
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL.Pattern;
using OldRod.Core.Memory;

namespace OldRod.Core.Ast.IL.Transform
{
    public class StackFrameTransform : IILAstTransform
    {
        public const string Tag = "StackFrameTransform";
        
        /* The following makes an assumption that each field in the stack frame
         * is referenced using the BP register. Forks of the virtualiser plugin
         * could deviate from this, using a different register or make more use
         * of registers in general.
         *
         * Newer versions of the devirtualiser could therefore benefit from a more
         * generic approach.
         * 
         */

        private static readonly ILSequencePattern<ILStatement> AllocateLocalsPattern =
            new ILSequencePattern<ILStatement>(
                // op0 = pushr_dword(sp)
                new ILAssignmentPattern(
                    ILVariablePattern.Any,
                    ILInstructionPattern.PushDwordReg(VMRegisters.SP)
                ),
                // op1 = pushi_dword(locals_count)
                new ILAssignmentPattern(
                    ILVariablePattern.Any,
                    ILInstructionPattern.PushAnyDword().Capture("push_local_count")
                ),
                // op2 = add_dword(op0, op1)
                new ILAssignmentPattern(
                    ILVariablePattern.Any,
                    new ILInstructionPattern(ILCode.ADD_DWORD, ILOperandPattern.Null,
                        ILVariablePattern.Any, ILVariablePattern.Any)
                ),
                // sp = op2
                new ILAssignmentPattern(VMRegisters.SP, ILVariablePattern.Any)
            );

        private static readonly ILSequencePattern<ILStatement> StoreToLocalPattern =
            new ILSequencePattern<ILStatement>(
                // op0 = pushr_dword(bp)
                new ILAssignmentPattern(
                    ILVariablePattern.Any,
                    ILInstructionPattern.PushDwordReg(VMRegisters.BP)
                ),
                // op1 = pushi_dword(offset)
                new ILAssignmentPattern(ILVariablePattern.Any,
                    ILInstructionPattern.PushAnyDword().Capture("push_offset")
                ),
                // op2 = add_dword(op0, op1)
                new ILAssignmentPattern(
                    ILVariablePattern.Any,
                    new ILInstructionPattern(ILCode.ADD_DWORD, ILOperandPattern.Null,
                        ILVariablePattern.Any, ILVariablePattern.Any)
                ),
                // sind_xxxx(value, op2)
                new ILExpressionStatementPattern(
                    new ILInstructionPattern(
                        new ILOpCodePattern(
                            ILCode.SIND_PTR, ILCode.SIND_BYTE,
                            ILCode.SIND_WORD, ILCode.SIND_DWORD,
                            ILCode.SIND_QWORD, ILCode.SIND_OBJECT),
                        ILOperandPattern.Null,
                        ILVariablePattern.Any, ILVariablePattern.Any)
                ).Capture("store")
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
                    ILVariablePattern.Any,
                    ILInstructionPattern.PushDwordReg(VMRegisters.BP)
                ),
                // op1 = pushi_dword(offset)
                new ILAssignmentPattern(ILVariablePattern.Any,
                    ILInstructionPattern.PushAnyDword().Capture("push_offset")
                ),
                // op2 = add_dword(op0, op1)
                new ILAssignmentPattern(
                    ILVariablePattern.Any,
                    new ILInstructionPattern(ILCode.ADD_DWORD, ILOperandPattern.Null,
                        ILVariablePattern.Any, ILVariablePattern.Any)
                ),
                // op3 = lind_xxxx(op2)
                new ILAssignmentPattern(
                    ILVariablePattern.Any,
                    new ILInstructionPattern(
                        new ILOpCodePattern(
                            ILCode.LIND_PTR, ILCode.LIND_BYTE,
                            ILCode.LIND_WORD, ILCode.LIND_DWORD,
                            ILCode.LIND_QWORD, ILCode.LIND_OBJECT),
                        ILOperandPattern.Null,
                        ILVariablePattern.Any
                    ).Capture("load")
                )
            );

        private static readonly ILSequencePattern<ILStatement> LoadLocalRefPattern =
            new ILSequencePattern<ILStatement>(
                // op0 = pushr_dword(bp)
                new ILAssignmentPattern(
                    ILVariablePattern.Any,
                    ILInstructionPattern.PushDwordReg(VMRegisters.BP)
                ),
                // r0 = op0
                new ILAssignmentPattern(ILVariablePattern.Any, ILVariablePattern.Any),
                // op1 = pushr_object(r0)
                new ILAssignmentPattern(ILVariablePattern.Any, ILInstructionPattern.PushAnyObjectReg()),
                // op2 = pushi_dword(offset)
                new ILAssignmentPattern(ILVariablePattern.Any,
                    ILInstructionPattern.PushAnyDword().Capture("push_offset")),
                // op3 = add_qword(op1, op2)
                new ILAssignmentPattern(
                    ILVariablePattern.Any,
                    new ILInstructionPattern(ILCode.ADD_QWORD, ILOperandPattern.Null,
                        ILVariablePattern.Any, ILVariablePattern.Any)
                ),
                // r0 = op3
                new ILAssignmentPattern(ILVariablePattern.Any, ILVariablePattern.Any.CaptureVar("final_value"))
            );
        
        public string Name => "Stack Frame Transform";

        public void ApplyTransformation(ILCompilationUnit unit, ILogger logger)
        {
            RemoveSPAssignments(unit);
            DetermineAndDeclareLocals(unit);
            ReplaceRawLocalReferences(unit, logger);
        }

        private static void RemoveSPAssignments(ILCompilationUnit unit)
        {
            // We assume all stack related operations are already handled. 
            // We can therefore safely remove any reference to SP.
            
            var sp = unit.GetOrCreateVariable("SP");
            foreach (var assign in sp.AssignedBy.ToArray())
            {
                assign.Variable = null;
                foreach (var use in assign.Value.AcceptVisitor(VariableUsageCollector.Instance))
                    use.Variable = null;
                assign.Remove();
            }
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
                    else if ((match = LoadLocalRefPattern.Match(block.Statements, i)).Success)
                        ReplaceLoadLocalRef(unit, match, logger);
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

        private static void ReplaceLoadLocalRef(ILCompilationUnit unit, MatchResult match, ILogger logger)
        {
            // Obtain variable that is referenced.
            var pushOffset = (ILInstructionExpression) match.Captures["push_offset"][0];
            int offset = unchecked((int) Convert.ToUInt32(pushOffset.Operand));
            var variable = ResolveVariable(unit, offset, logger);

            // Obtain reference to final loaded value of the variable. 
            var finalValue = (ILVariableExpression) match.Captures["final_value"][0];
            finalValue.Variable = null;
            
            // Replace with normal variable expression.
            finalValue.ReplaceWith(new ILVariableExpression(variable)
            {
                IsReference = true,
                ExpressionType = VMType.Object
            });
        }

        private static ILVariable ResolveVariable(ILCompilationUnit unit, int offset, ILogger logger)
        {
            var field = unit.FrameLayout.Resolve(offset);
            if (!field.IsValid)
            {
                switch (field.FieldKind)
                {
                    case FrameFieldKind.Parameter:
                        logger.Warning(Tag, $"Reference to non-existing parameter {field.Index} detected.");
                        break;
                    case FrameFieldKind.ReturnAddress:
                        logger.Warning(Tag, $"Reference to return address detected.");
                        break;
                    case FrameFieldKind.CallersBasePointer:
                        logger.Warning(Tag, $"Reference to callers base pointer detected.");
                        break;
                    case FrameFieldKind.LocalVariable:
                        logger.Warning(Tag, $"Reference to non-existing local variable {field.Index} detected.");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return unit.GetOrCreateVariable(field);
        }
    }
}