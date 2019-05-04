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
using AsmResolver.Net.Signatures;

namespace OldRod.Core.Ast.Cil
{
    public class CilVariable
    {
        public CilVariable(string name, TypeSignature variableType)
        {
            Name = name;
            VariableType = variableType;
        }
        
        public string Name
        {
            get;
        }

        public TypeSignature VariableType
        {
            get;
            set;
        }

        public IList<CilVariableExpression> UsedBy
        {
            get;
        } = new List<CilVariableExpression>();

        public IList<CilAssignmentStatement> AssignedBy
        {
            get;
        } = new List<CilAssignmentStatement>();

        public override string ToString()
        {
            return $"{VariableType} {Name}";
        }
        
    }

    public class CilParameter : CilVariable
    {
        public int ParameterIndex
        {
            get;
        }

        public CilParameter(string name, TypeSignature variableType, int parameterIndex)
            : base(name, variableType)
        {
            ParameterIndex = parameterIndex;
        }
    }
    
}