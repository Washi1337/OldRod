using System.Collections.Generic;
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL
{
    public class ILFlagsVariable : ILVariable
    {
        public ILFlagsVariable(string name, IEnumerable<int> dataOffsets) 
            : base(name)
        {
            VariableType = VMType.Byte;
            DataSources.UnionWith(dataOffsets);
        }

        public ISet<int> DataSources
        {
            get;
        } = new HashSet<int>();

        public ICollection<ILExpression> ImplicitAssignments
        {
            get;
        } = new HashSet<ILExpression>();
    }
}