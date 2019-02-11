using System.Collections.Generic;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler
{
    public class RecompilerContext
    {
        public RecompilerContext(MetadataImage targetImage, ILToCilRecompiler recompiler)
        {
            TargetImage = targetImage;
            Recompiler = recompiler;
            ReferenceImporter = new ReferenceImporter(targetImage);
        }

        public MetadataImage TargetImage
        {
            get;
        }

        public ILToCilRecompiler Recompiler
        {
            get;
        }

        public ReferenceImporter ReferenceImporter
        {
            get;
        }
        
        public IDictionary<ILVariable, VariableSignature> Variables
        {
            get;
        } = new Dictionary<ILVariable, VariableSignature>();
    }
}