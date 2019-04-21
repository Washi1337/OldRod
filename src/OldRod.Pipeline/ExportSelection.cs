using System.Collections.Generic;
using OldRod.Core.Architecture;

namespace OldRod.Pipeline
{
    public abstract class ExportSelection
    {
        public static readonly ExportSelection All = new AllExportsSelection();

        public abstract bool Contains(uint exportId, VMExportInfo exportInfo);

        private sealed class AllExportsSelection : ExportSelection
        {
            public override bool Contains(uint exportId, VMExportInfo exportInfo) => true;
        }
    }
    
    public class ExclusionExportSelection : ExportSelection
    {
        public ISet<uint> ExcludedExports
        {
            get;
        } = new HashSet<uint>();

        public override bool Contains(uint exportId, VMExportInfo exportInfo)
        {
            return !ExcludedExports.Contains(exportId);
        }
    }

    public class IncludedExportSelection : ExportSelection
    {
        public ISet<uint> IncludedExports
        {
            get;
        } = new HashSet<uint>();
        
        public override bool Contains(uint exportId, VMExportInfo exportInfo)
        {
            return IncludedExports.Contains(exportId);
        }
    }
}