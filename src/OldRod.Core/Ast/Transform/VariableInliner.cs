using System;
using System.Linq;

namespace OldRod.Core.Ast.Transform
{
    public class VariableInliner : IAstTransform, IILAstVisitor<bool>
    {
        private ILCompilationUnit _currentUnit;
        private readonly VariableUsageCollector _collector = new VariableUsageCollector();
        
        public void ApplyTransformation(ILCompilationUnit unit)
        {
            _currentUnit = unit;
            while (unit.AcceptVisitor(this))
            {
                // Repeat until no more changes.
            }
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
            bool changed = false;

            for (var i = 0; i < block.Statements.Count; i++)
            {
                var statement = block.Statements[i];
                if (statement is ILAssignmentStatement assignmentStatement)
                {
                    var variable = assignmentStatement.Variable;
                    switch (variable.UsedBy.Count)
                    {
                        case 0:
                        {
                            var embeddedReferences = assignmentStatement.Value.AcceptVisitor(_collector);
                            foreach (var reference in embeddedReferences)
                                reference.Variable.UsedBy.Remove(reference);
                        
                            block.Statements.RemoveAt(i);
                            i--;
                            changed = true;
                            break;
                        }
                        case 1:
                        {
                            variable.UsedBy[0].ReplaceWith(assignmentStatement.Value.Remove());
                            variable.UsedBy.Clear();
                            block.Statements.RemoveAt(i);
                            i--;
                            changed = true;
                            break;
                        }
                    }
                }
                else
                {
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