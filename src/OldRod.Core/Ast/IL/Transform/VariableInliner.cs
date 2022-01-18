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

using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL.Pattern;

namespace OldRod.Core.Ast.IL.Transform
{
    public class VariableInliner : ChangeAwareILAstTransform
    {
        private static readonly ILInstructionPattern PushPattern = ILAstPattern
            .Instruction(ILCode.PUSHR_BYTE, ILCode.PUSHR_WORD,
                ILCode.PUSHR_DWORD, ILCode.PUSHR_QWORD,
                ILCode.PUSHR_OBJECT)
            .WithAnyOperand()
            .WithArguments(ILExpressionPattern.Any);

        public override string Name => "Variable Inlining";

        public override bool ApplyTransformation(ILCompilationUnit unit, ILogger logger)
        {
            return base.ApplyTransformation(unit, logger) && unit.RemoveNonUsedVariables();
        }
        
        public override bool VisitBlock(ILAstBlock block)
        {
            // Find all assignments of variables, and count the amount of usages for each variable.
            // If the variable is not used it can be removed. If it is only used once, it can be inlined.
            
            bool changed = false;
            for (var i = 0; i < block.Statements.Count; i++)
            {
                // Find assignment statement:
                var statement = block.Statements[i];
                if (statement is ILAssignmentStatement assignmentStatement && !(assignmentStatement.Variable is ILParameter))
                {
                    bool removeStatement = true;
                    var usages = assignmentStatement.Variable.UsedBy;
                 
                    // Count usages.
                    switch (usages.Count)
                    {
                        case 0:
                        {
                            if (assignmentStatement.Variable is ILFlagsVariable)
                            {
                                removeStatement = false;
                            }
                            if (assignmentStatement.Value.HasPotentialSideEffects)
                            {
                                // If the value has side effects, it cannot be removed.
                                removeStatement = false;
                                assignmentStatement.ReplaceWith(new ILExpressionStatement((ILExpression) assignmentStatement.Value.Remove()));
                            }
                            else
                            {
                                // Find all variables that are referenced in the statement, and remove them from the 
                                // usage lists.
                                var embeddedReferences = assignmentStatement.Value.AcceptVisitor(VariableUsageCollector.Instance);
                                foreach (var reference in embeddedReferences)
                                    reference.Variable.UsedBy.Remove(reference);
                            }

                            break;
                        }
                        case 1 when assignmentStatement.Variable.IsVirtual
                            // We cannot inline into phi nodes.
                            && !(usages[0].Parent is ILPhiExpression)
                            // We also cannot insert phi nodes in arbitrary expressions other than assignments.
                            && !(assignmentStatement.Value is ILPhiExpression)
                            // Finally, we cannot inline expressions with side effects => depend on order of execution.
                            && (!assignmentStatement.Value.HasPotentialSideEffects || !HasNoSideEffectsInBetween(assignmentStatement, usages[0])):
                        {
                            // Inline the variable's value.
                            InlineVariable(usages[0], assignmentStatement);
                            usages.Clear();
                            break;
                        }
                        default:
                        {
                            removeStatement = false;
                            break;
                        }
                    }

                    if (removeStatement)
                    {
                        // We applied a transformation, remove the original statement.
                        block.Statements.RemoveAt(i);
                        i--;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static void InlineVariable(ILVariableExpression usage, ILAssignmentStatement assignmentStatement)
        {
            var replacement = assignmentStatement.Value;
            
            // Simple inlining can cause massive PUSH chains. For example, the following:
            //
            //     R0 = PUSHR_DWORD(expr)
            //     R1 = PUSHR_DWORD(R0)
            //
            // would be optimised to
            //
            //     R1 = PUSHR_DWORD(PUSHR_DWORD(expr))
            //
            // But this can be simply:
            //
            //     R1 = PUSHR_DWORD(expr)
            //
            // Try to optimise for this:
            var match1 = PushPattern.Match(usage.Parent);
            var match2 = PushPattern.Match(assignmentStatement.Value);
            if (match1.Success && match2.Success)
            {
                var pushVariable = (ILInstructionExpression) usage.Parent;
                var value = (ILInstructionExpression) assignmentStatement.Value;

                if (pushVariable.OpCode.Code == value.OpCode.Code)
                    replacement = value.Arguments[0];
            }

            usage.Variable = null;
            usage.ReplaceWith(replacement.Remove());
        }

        private static bool HasNoSideEffectsInBetween(ILStatement statement, ILExpression expression)
        {
            // Check if all expressions that are evaluated before the provided expression in the same containing statement
            // have potential side effects.  
            var currentExpression = expression;
            while (true)
            {
                // Obtain the parent expression that contains the argument.
                var parentExpression = currentExpression.Parent as IILArgumentsProvider;
                if (parentExpression == null)
                    break;

                // Figure out if all arguments evaluated before the current expression have any potential side effects.
                for (int i = 0; parentExpression.Arguments[i] != currentExpression; i++)
                {
                    if (i >= parentExpression.Arguments.Count || parentExpression.Arguments[i].HasPotentialSideEffects)
                        return true;
                }

                currentExpression = (ILExpression) parentExpression;
            }

            // Verify that the two statements occur in the same block.
            var statement2 = (ILStatement) currentExpression.Parent;
            var block = (ILAstBlock) statement2.Parent;

            if ((ILAstBlock) statement.Parent != block)
                return true;

            // Start at the first statement, and move up till we find the second statement containing the expression,
            // and figure out if any of the statements in between have potential side effects.
            int startIndex = block.Statements.IndexOf(statement);
            for (int i = startIndex + 1; block.Statements[i] != statement2; i++)
            {
                if (i >= block.Statements.Count || block.Statements[i].HasPotentialSideEffects)
                    return true;
            }
            
            // Nothing has been found that could cause side effects.
            return false;
        }

    }
}