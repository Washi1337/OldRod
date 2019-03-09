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

using System.Collections.Generic;
using System.Linq;
using Rivers;
using Rivers.Analysis;

namespace OldRod.Core.Ast.IL.Transform
{
    // Algorithm based on:
    // http://staff.cs.upt.ro/~chirila/teaching/upt/c51-pt/aamcij/7113/Fly0142.html
    
    public class SsaTransform : IILAstTransform
    {
        private static readonly VariableUsageCollector Collector = new VariableUsageCollector();
       
        public string Name => "Static Single Assignment Transform";

        public void ApplyTransformation(ILCompilationUnit unit, ILogger logger)
        {
            var phiNodes = InsertPhiNodes(unit);
            RenameVariables(unit, phiNodes);
        }

        private static Dictionary<Node, ICollection<ILAssignmentStatement>> InsertPhiNodes(ILCompilationUnit unit)
        {
            var result = unit.ControlFlowGraph.Nodes.ToDictionary(
                x => x, 
                x => (ICollection<ILAssignmentStatement>) new List<ILAssignmentStatement>());
            
            // We try to find all variables that have more than one assignment, and therefore have multiple
            // versions of it during execution of the program. This is only a problem when they have different
            // values at join nodes, as depicted below. We therefore need to get to the dominance frontier of
            // those nodes and insert phi nodes there. 
            //
            //  [ x1 <- value1 ]         [ x2 <- value2 ]
            //        |                        |
            //        '------------+-----------'
            //                     |
            //          [ x3 <- phi(x1, x2) ]
            //
            
            // Collect all nodes that contain a variable assignment (i.e. a new definition).
            var variableBlocks = unit.Variables.ToDictionary(
                x => x,
                x => new HashSet<Node>(x.AssignedBy.Select(a => a.GetParentNode())));
            
            foreach (var variable in unit.Variables.Where(x => x.AssignedBy.Count > 1))
            {
                var agenda = new Queue<Node>(variableBlocks[variable]);
                while (agenda.Count > 0)
                {
                    var current = agenda.Dequeue();
                    foreach (var frontierNode in unit.DominatorInfo.GetDominanceFrontier(current))
                    {
                        // If the frontier node does not define a phi node already for this variable, we need to add it.
                        if (result[frontierNode].All(x => x.Variable != variable))
                        {
                            // Check if the variable is defined in the frontier node.
                            bool defined = variableBlocks[variable].Contains(frontierNode);

                            // Build phi node.
                            // The number of different versions of the variable is equal to the amount of predecessors. 
                            var phiExpression = new ILPhiExpression(Enumerable
                                .Repeat(variable, frontierNode.InDegree)
                                .Select(v => new ILVariableExpression(v)));
                            
                            var phiNode = new ILAssignmentStatement(variable, phiExpression);

                            // Insert at top of the current block.
                            var block = (ILAstBlock) frontierNode.UserData[ILAstBlock.AstBlockProperty];
                            block.Statements.Insert(0, phiNode);
                            
                            // Register phi node.
                            result[frontierNode].Add(phiNode);

                            // We might have to check this node again if we introduce a new version of this variable
                            // at this node.
                            if (!defined)
                                agenda.Enqueue(frontierNode);
                        }
                    }
                }
            }

            return result;
        }

        private static void RenameVariables(ILCompilationUnit unit,
            IDictionary<Node, ICollection<ILAssignmentStatement>> phiNodes)
        {
            // We keep track of two variables for each variable.
            // - A counter to introduce new variables with new names that are unique throughout the entire function.
            // - A stack containing the current versions of the variable.
            var counter = new Dictionary<ILVariable, int>();
            var stack = new Dictionary<ILVariable, Stack<ILVariable>>();

            foreach (var variable in unit.Variables.Union(unit.Parameters))
            {
                counter[variable] = 0;
                stack[variable] = new Stack<ILVariable>();
                
                // Note: This is a slight deviation of the original algorithm.
                // Some variables (such as registers) do not have an initial value specified in the method.
                // To avoid problems, we add the "global" definition to the stack.
                stack[variable].Push(variable); 
            }
            
            // Start at the entry point of the graph.
            Rename(unit.ControlFlowGraph.Entrypoint);
            
            void Rename(Node n)
            {
                var block = (ILAstBlock) n.UserData[ILAstBlock.AstBlockProperty];
                var originalVars = new Dictionary<ILAssignmentStatement, ILVariable>();
                
                foreach (var statement in block.Statements)
                {
                    bool updateVariables = true;
                    
                    if (statement is ILAssignmentStatement assignment)
                    {
                        var variable = assignment.Variable;
                        originalVars.Add(assignment, variable);
                    
                        // We have a new version of a variable. Let's introduce a new version.
                        counter[variable]++;
                        var newVariable = unit.GetOrCreateVariable(variable.Name + "_v" + counter[variable]);
                        newVariable.VariableType = variable.VariableType;
                        stack[variable].Push(newVariable);

                        // Update the variable in the assignment.
                        assignment.Variable = newVariable;
                        
                        // Don't update arguments of phi nodes. They are updated somewhere else.
                        if (assignment.Value is ILPhiExpression) 
                            updateVariables=false;
                    }

                    if (updateVariables)
                    {
                        // Update variables inside the statement with the new versions.
                        foreach (var use in statement.AcceptVisitor(Collector))
                            use.Variable = stack[use.Variable].Peek();
                    }
                }

                // Update phi statements in successor nodes.
                foreach (var successor in n.GetSuccessors())
                {
                    // Determine the index of the phi expression argument. 
                    // TODO: Might be inefficient to do an OrderBy every time.
                    //       Maybe optimise by ordering (e.g. numbering) the edges beforehand?
                    int argumentIndex = successor.GetPredecessors().OrderBy(x => x.Name).ToList().IndexOf(n);
                    
                    // Update all variables in the phi nodes to the new versions.
                    foreach (var phiNode in phiNodes[successor])
                    {
                        var phiExpression = (ILPhiExpression) phiNode.Value;
                        var oldVariable = phiExpression.Variables[argumentIndex].Variable;
                        var newVariable = stack[oldVariable].Peek();
                        phiExpression.Variables[argumentIndex].Variable = newVariable;
                    }
                }

                foreach (var child in unit.DominatorTree.Nodes[n.Name].GetSuccessors())
                    Rename(unit.ControlFlowGraph.Nodes[child.Name]);

                // We are done with the newly introduced variables.
                // Pop all new versions of the variable from their stacks.
                foreach (var entry in originalVars)
                    stack[entry.Value].Pop();
            }
        }
        
    }
}