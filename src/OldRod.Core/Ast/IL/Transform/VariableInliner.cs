using System;
using System.Linq;

namespace OldRod.Core.Ast.IL.Transform
{
    public class VariableInliner : IAstTransform, IILAstVisitor<bool>
    {
        private readonly VariableUsageCollector _collector = new VariableUsageCollector();

        public string Name => "Variable Inlining";

        public void ApplyTransformation(ILCompilationUnit unit)
        {
            while (unit.AcceptVisitor(this))
            {
                // Repeat until no more changes.
            }
            unit.RemoveNonUsedVariables();
        }

        public bool VisitCompilationUnit(ILCompilationUnit unit)
        {
            bool changed = false;
            foreach (var node in unit.ControlFlowGraph.Nodes.OrderBy(x=>x.Name))
            {
                var block = (ILAstBlock) node.UserData[ILAstBlock.AstBlockProperty];
                changed |= block.AcceptVisitor(this);
            }

            return changed;
        }
        
        public bool VisitBlock(ILAstBlock block)
        {
            // Find all assignments of variables, and count the amount of usages for each variable.
            // If the variable is not used it can be removed. If it is only used once, it can be inlined.
            
            bool changed = false;
            for (var i = 0; i < block.Statements.Count; i++)
            {
                // Find assignment statement:
                var statement = block.Statements[i];
                if (statement is ILAssignmentStatement assignmentStatement)
                {
                    bool appliedTransform = true;
                    var usages = assignmentStatement.Variable.UsedBy;
                    
                    // Count usages.
                    switch (usages.Count)
                    {
                        case 0:
                        {
                            // Find all variables that are referenced in the statement, and remove them from the 
                            // usage lists.
                            var embeddedReferences = assignmentStatement.Value.AcceptVisitor(_collector);
                            foreach (var reference in embeddedReferences)
                                reference.Variable.UsedBy.Remove(reference);
                            break;
                        }
                        case 1:
                        {
                            // Inline the variable's value.
                            
                            usages[0].ReplaceWith(assignmentStatement.Value.Remove());
                            usages.Clear();
                            break;
                        }
                        default: 
                            appliedTransform = false;
                            break;
                    }

                    if (appliedTransform)
                    {
                        // We applied a transformation, remove the original statement.
                        block.Statements.RemoveAt(i);
                        i--;
                        changed = true;
                    }
                }
                else
                {
                    // Search deeper.
                    changed |= statement.AcceptVisitor(this);
                }
            }

            return changed;
        }

        public bool VisitExpressionStatement(ILExpressionStatement statement)
        {
            return false;
        }

        public bool VisitAssignmentStatement(ILAssignmentStatement statement)
        {
            return false;
        }

        public bool VisitInstructionExpression(ILInstructionExpression expression)
        {
            return false;
        }

        public bool VisitVariableExpression(ILVariableExpression expression)
        {
            return false;
        }

        public bool VisitVCallExpression(ILVCallExpression expression)
        {
            return false;
        }
    }
}