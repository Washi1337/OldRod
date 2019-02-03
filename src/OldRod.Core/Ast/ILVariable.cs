using System.Collections.Generic;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;

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

        public TypeSignature VariableType
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