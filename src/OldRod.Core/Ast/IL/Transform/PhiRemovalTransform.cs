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
using System.Collections.Generic;
using System.Linq;

namespace OldRod.Core.Ast.IL.Transform
{
    public class PhiRemovalTransform : IILAstTransform
    {
        private sealed class PhiCongruenceClass
        {
            public PhiCongruenceClass(ILVariable representative)
            {
                Representative = representative ?? throw new ArgumentNullException(nameof(representative));
            }
            
            public ILVariable Representative
            {
                get;
            }

            public ISet<ILVariable> Variables
            {
                get;
            } = new HashSet<ILVariable>();

            public void ReplaceVarsWithRepresentative()
            {
                foreach (var variable in Variables)
                {
                    foreach (var use in variable.UsedBy.ToArray())
                        use.Variable = Representative;
                    foreach (var assign in variable.AssignedBy.ToArray())
                        assign.Variable = Representative;
                }   
            }
            
            public void RemovePhiNodes()
            {
                foreach (var assign in Representative.AssignedBy.ToArray())
                {
                    if (assign.Value is ILPhiExpression phi)
                    {
                        if (phi.Variables.Any(x => x.Variable != Representative))
                        {
                            // Should never happen. If it does, we have a faulty algorithm :L
                            throw new ILAstBuilderException("Attempted to remove a phi node that still has unresolved variable references.");
                        }

                        assign.Remove();
                    }
                }
            }
            
            public override string ToString()
            {
                return "{" + string.Join(", ", Variables.Select(x => x.Name)) + "}";
            }
        }
        
        public string Name => "Phi Removal";

        public void ApplyTransformation(ILCompilationUnit unit, ILogger logger)
        {
            var classes = ObtainPhiCongruenceClasses(unit);

            foreach (var @class in classes)
            {
                @class.ReplaceVarsWithRepresentative();
                @class.RemovePhiNodes();
            }
        }

        private static IEnumerable<PhiCongruenceClass> ObtainPhiCongruenceClasses(ILCompilationUnit unit)
        {
            var classes = new HashSet<PhiCongruenceClass>();
            var variableToClass = new Dictionary<ILVariable, PhiCongruenceClass>();
            foreach (var variable in unit.Variables.ToArray())
            {
                // Phi nodes are always present in the form:
                // v = phi(v1, v2, ..., vn)
                
                // A situation like the following might happen:
                // 
                // a3 = phi(a1, a2)
                // ...
                // a5 = phi(a3, a5)
                //
                // Here, a3 and a5 are connected and form an equivalence class.
                //
                
                if (variable.AssignedBy.Count == 1 && variable.AssignedBy[0].Value is ILPhiExpression phi)
                {
                    // Check if variable does not belong to any equivalence class already.
                    var connectedVariables = new HashSet<ILVariable>(phi.Variables.Select(x => x.Variable)) {variable};

                    var congruenceClasses = new List<PhiCongruenceClass>();
                    foreach (var connectedVar in connectedVariables.ToArray())
                    {
                        if (variableToClass.TryGetValue(connectedVar, out var congruenceClass))
                        {
                            // The referenced variable is already part of another class, we need to combine the
                            // two classes together. 
                            congruenceClasses.Add(congruenceClass);
                            classes.Remove(congruenceClass);
                            connectedVariables.UnionWith(congruenceClass.Variables);
                        }
                    }

                    PhiCongruenceClass finalClass;
                    if (congruenceClasses.Count == 0)
                    {
                        // No variable was part of a class yet => We need a new one.
                        var representative = unit.GetOrCreateVariable("phi_" + variableToClass.Count);
                        representative.VariableType = variable.VariableType;
                        finalClass = new PhiCongruenceClass(representative);
                    }
                    else
                    {
                        // At least one of the variables was part of a class already.
                        // Pick one class that we are going to expand.
                        finalClass = congruenceClasses[0];
                    }

                    // Add all connected variables to the new class.
                    finalClass.Variables.UnionWith(connectedVariables);
                    foreach (var connectedVar in connectedVariables)
                        variableToClass[connectedVar] = finalClass;

                    classes.Add(finalClass);
                }
            }

            return classes;
        }

    }
}