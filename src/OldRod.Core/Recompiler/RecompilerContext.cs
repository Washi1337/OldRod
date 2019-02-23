using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler
{
    public class RecompilerContext
    {
        public RecompilerContext(CilMethodBody methodBody, MetadataImage targetImage,
            ILToCilRecompiler recompiler)
        {
            MethodBody = methodBody;
            TargetImage = targetImage;
            Recompiler = recompiler;
            ReferenceImporter = new ReferenceImporter(targetImage);
        }

        public CilMethodBody MethodBody
        {
            get;
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

        public IDictionary<ILParameter, ParameterSignature> Parameters
        {
            get;
        } = new Dictionary<ILParameter, ParameterSignature>();
        
        public VariableSignature FlagVariable
        {
            get;
            set;
        }
    }
}