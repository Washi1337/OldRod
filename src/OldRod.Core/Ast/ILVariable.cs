using System.Collections.Generic;

namespace OldRod.Core.Ast
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