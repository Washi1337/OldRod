using System.Collections.Generic;
using System.Linq;
using Carp.Core.Architecture;

namespace Carp.Core.Disassembly
{
    public class ValueReference
    {
        public ValueReference()
        {   
        }
        
        public ValueReference(ILInstruction dataSource)
        {
            DataSources.Add(dataSource);
        }

        private ValueReference(IEnumerable<ILInstruction> dataSources)
        {
            DataSources.UnionWith(dataSources);
        }

        public ISet<ILInstruction> DataSources
        {
            get;
        } = new HashSet<ILInstruction>();

        public bool IsUnknown => DataSources.Count == 0;

        public bool MergeWith(ValueReference value)
        {
            int size = DataSources.Count;
            DataSources.UnionWith(value.DataSources);
            return size != DataSources.Count;
        }
        
        public ValueReference Copy()
        {
            return new ValueReference(DataSources);
        }
        
        public override string ToString()
        {
            return IsUnknown
                ? "?"
                : string.Join(" | ", DataSources.Select(x => x.Offset.ToString("X4")));
        }
    }
}