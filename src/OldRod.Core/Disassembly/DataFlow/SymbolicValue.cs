using System.Collections.Generic;
using System.Linq;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.DataFlow
{
    public class SymbolicValue
    {
        public SymbolicValue()
        {   
        }
        
        public SymbolicValue(ILInstruction dataSource)
        {
            DataSources.Add(dataSource);
        }

        private SymbolicValue(IEnumerable<ILInstruction> dataSources)
        {
            DataSources.UnionWith(dataSources);
        }

        public ISet<ILInstruction> DataSources
        {
            get;
        } = new HashSet<ILInstruction>();

        public bool IsUnknown => DataSources.Count == 0;
        
        public bool MergeWith(SymbolicValue value)
        {
            int size = DataSources.Count;
            DataSources.UnionWith(value.DataSources);
            return size != DataSources.Count;
        }
        
        public SymbolicValue Copy()
        {
            return new SymbolicValue(DataSources);
        }
        
        public override string ToString()
        {
            return IsUnknown
                ? "?"
                : string.Join(" | ", DataSources.Select(x => x.Offset.ToString("X4")));
        }
    }
}