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
    public class PhiRemovalTransform : IAstTransform
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
                            // should never happen.
                            throw new InvalidOperationException();
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

        public void ApplyTransformation(ILCompilationUnit unit)
        {
            var classes = ObtainPhiCongruenceClasses(unit);

            foreach (var @class in classes)
            {
                @class.ReplaceVarsWithRepresentative();
                @class.RemovePhiNodes();
            }
        }

        private static List<PhiCongruenceClass> ObtainPhiCongruenceClasses(ILCompilationUnit unit)
        {
            var classes = new List<PhiCongruenceClass>();
            foreach (var variable in unit.Variables.ToArray())
            {
                if (variable.AssignedBy.Count == 1
                    && variable.AssignedBy[0].Value is ILPhiExpression
                    && variable.UsedBy.All(x => !(x.Parent is ILPhiExpression)))
                {
                    var representative = unit.GetOrCreateVariable("phi_" + classes.Count);
                    representative.VariableType = variable.VariableType;

                    var congruenceClass = new PhiCongruenceClass(representative);
                    congruenceClass.Variables.UnionWith(CollectLinkedVariables(variable));
                    classes.Add(congruenceClass);
                }
            }

            return classes;
        }


        private static ICollection<ILVariable> CollectLinkedVariables(ILVariable variable)
        {
            var result = new HashSet<ILVariable>();
            result.Add(variable);

            foreach (var assign in variable.AssignedBy)
            {
                if (assign.Value is ILPhiExpression phi)
                {
                    foreach (var parameter in phi.Variables)
                        result.UnionWith(CollectLinkedVariables(parameter.Variable));
                }
            }

            return result;
        }

    }
}