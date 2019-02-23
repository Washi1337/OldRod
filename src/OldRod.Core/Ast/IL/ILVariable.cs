using System.Collections;
using System.Collections.Generic;
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL
{
    public class ILVariable
    {
        public ILVariable(string name)
            : this(name, false)
        {
        }

        public ILVariable(string name, bool isParameter)
        {
            Name = name;
            IsParameter = isParameter;
        }
        
        public string Name
        {
            get;
            set;
        }

        public VMType VariableType
        {
            get;
            set;
        }

        public bool IsParameter
        {
            get;
        }

        public IList<ILAssignmentStatement> AssignedBy
        {
            get;
        } = new List<ILAssignmentStatement>();
        
        public IList<ILVariableExpression> UsedBy
        {
            get;
        } = new List<ILVariableExpression>();

        public override string ToString()
        {
            return Name;
        }
    }
}