using System.Collections.Generic;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Ast;

namespace OldRod.Core.Recompiler
{
    public class ILAstCompiler
    {
        private readonly MetadataImage _image;

        public ILAstCompiler(MetadataImage image)
        {
            _image = image;
        }
        
        public CilMethodBody Compile(MethodDefinition method, ILCompilationUnit unit)
        {
            var visitor = new ILAstToCilVisitor(_image);

            var methodBody = new CilMethodBody(method);

            methodBody.Instructions.AddRange(unit.AcceptVisitor(visitor));
            if (visitor.Variables.Count > 0)
            {
                methodBody.Signature = new StandAloneSignature(new LocalVariableSignature(visitor.Variables));
                methodBody.InitLocals = true;
            }
            
            return methodBody;
        }
    }
}