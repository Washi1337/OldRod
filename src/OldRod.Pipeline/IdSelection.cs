using System.Collections.Generic;

namespace OldRod.Pipeline
{
    public abstract class IdSelection
    {
        public static readonly IdSelection All = new AllIdsSelection();

        public abstract bool Contains(uint id);

        private sealed class AllIdsSelection : IdSelection
        {
            public override bool Contains(uint id) => true;
        }
    }
    
    public class ExclusionIdSelection : IdSelection
    {
        public ISet<uint> ExcludedIds
        {
            get;
        } = new HashSet<uint>();

        public override bool Contains(uint id)
        {
            return !ExcludedIds.Contains(id);
        }
    }

    public class IncludedIdSelection : IdSelection
    {
        public ISet<uint> IncludedIds
        {
            get;
        } = new HashSet<uint>();
        
        public override bool Contains(uint id)
        {
            return IncludedIds.Contains(id);
        }
    }
}