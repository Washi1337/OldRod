using System.Collections.Generic;
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL
{
    public class ILVariable
    {
        public ILVariable(string name)
        {
            Name = name;
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
        
        public IList<ILExpression> UsedBy
        {
            get;
        } = new List<ILExpression>();

        public override string ToString()
        {
            return Name;
        }
    }
}