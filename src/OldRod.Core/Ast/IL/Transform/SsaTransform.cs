using System.Collections.Generic;
using System.Linq;

namespace OldRod.Core.Ast.IL.Transform
{
    public class SsaTransform : IAstTransform
    {
        public string Name => "Static single assignment transformation";

        public void ApplyTransformation(ILCompilationUnit unit)
        {
            var derivatives = new Dictionary<ILVariable, ICollection<ILVariable>>();
            
            foreach (var variable in unit.Variables.ToArray())
            {
                var newVariables = new List<ILVariable>();
                derivatives.Add(variable, newVariables);
                
                // Introduce for each new assignment a new "version" of the variable
                // and update all references to the old variable in the dominated nodes
                // to the new one.
                foreach (var assignment in variable.AssignedBy.ToArray())
                {
                    var newVariable = unit.GetOrCreateVariable(variable.Name + "_v" + newVariables.Count);
                    newVariable.VariableType = variable.VariableType;
                    newVariables.Add(newVariable);
                    UpdateVariablesInDominatedNodes(unit, assignment, newVariable);
                }
            }
            
            // TODO: insert phi nodes for each derivative variable at the dominance frontier.
        }

        private void UpdateVariablesInDominatedNodes(ILCompilationUnit unit, ILAssignmentStatement assignment, ILVariable newVariable)
        {
            var oldVariable = assignment.Variable;
            var node = assignment.GetParentNode();
            var dominatedNodes = unit.DominatorInfo.GetDominatedNodes(node);

            // Update all dominated expressions that use the old variable.
            foreach (var use in oldVariable.UsedBy.ToArray())
            {
                var useNode = use.GetParentNode();
                if (dominatedNodes.Contains(useNode) && (node != useNode || HasExecutionOrder(assignment, use))) 
                    use.Variable = newVariable;
            }

            // Update all dominated statements that assign a new value to the variable.
            // Note that the new variables in these assignments are not final. However, this allows for
            // easier matching for the remaining assignments later.
            foreach (var assign in oldVariable.AssignedBy.ToArray())
            {
                var assignNode = assign.GetParentNode();
                if (dominatedNodes.Contains(assignNode) && (node != assignNode || HasExecutionOrder(assignment, assign)))
                    assign.Variable = newVariable;
            }

            assignment.Variable = newVariable;
        }

        private bool HasExecutionOrder(ILStatement first, ILAstNode second)
        {
            // TODO: Maybe use OriginalOffset instead to have a more efficient comparison?
            //       This might not be the best idea, as other transforms that may appear before
            //       this transform could be switching nodes around, resulting in the offset
            //       not being a representative for execution order anymore.
            //
            //       We can only do this if either all transforms before SSA are only reordering
            //       nodes without affecting any of the offsets, or they update the offsets
            //       accordingly.
            
            var block = (ILAstBlock) first.Parent; // Parent of a statement is always a block.
            int index = block.Statements.IndexOf(first);

            // Find the statement the other node is embedded in.
            var current = second;
            while (!(current is ILStatement))
                current = current.Parent;
            
            // Check if the marker is before the other enclosing statement.
            return index < block.Statements.IndexOf((ILStatement) current);
        }
    }
}