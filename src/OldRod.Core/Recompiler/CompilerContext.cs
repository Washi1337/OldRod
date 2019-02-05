using System.Collections.Generic;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Ast;
using Rivers;

namespace OldRod.Core.Recompiler
{
    public class CompilerContext
    {
        public CompilerContext(MetadataImage targetImage)
        {
            TargetImage = targetImage;
            ReferenceImporter = new ReferenceImporter(targetImage);
        }

        public MetadataImage TargetImage
        {
            get;
        }

        public ReferenceImporter ReferenceImporter
        {
            get;
        }

        public ILAstToCilVisitor CodeGenerator
        {
            get;
            set;
        }

        public IDictionary<Node, CilInstruction> BlockHeaders
        {
            get;
        } = new Dictionary<Node, CilInstruction>();

        public IDictionary<ILVariable, VariableSignature> Variables
        {
            get;
        } = new Dictionary<ILVariable, VariableSignature>();
    }
}