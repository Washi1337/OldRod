using System.Linq;

namespace OldRod.Core.Ast.IL.Transform
{
    public class VariableInliner : IILAstTransform, IILAstVisitor<bool>
    {
        private readonly VariableUsageCollector _collector = new VariableUsageCollector();

        public string Name => "Variable Inlining";

        public void ApplyTransformation(ILCompilationUnit unit, ILogger logger)
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
                if (statement is ILAssignmentStatement assignmentStatement && !(assignmentStatement.Variable is ILParameter))
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
                        case 1 when
                            // We cannot inline into phi nodes.
                            !(usages[0].Parent is ILPhiExpression)
                            // We also cannot insert phi nodes in arbitrary expressions other than assignments.
                            && !(assignmentStatement.Value is ILPhiExpression)
                            // Finally, we cannot inline expressions with side effects => depend on order of execution.
                            // TODO: This statement is not necessarily true, and has potential for improvement.
                            // Example:
                            //       ...
                            //       a = f()
                            //       b = g(arg[0], ... , arg[i-1], a, arg[i+1], ..., arg[n-1])
                            //       ...
                            // Provided that f() has side effects, but arg[0] till arg[i-1] not, it is still possible
                            // to inline, and thus further optimise the amount of variables. Potential solution is to
                            // compute a dependency graph for expressions as well to determine which expressions depend
                            // on order.
                            && !assignmentStatement.Value.HasPotentialSideEffects:
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

        public bool VisitPhiExpression(ILPhiExpression expression)
        {
            return false;
        }
    }
}