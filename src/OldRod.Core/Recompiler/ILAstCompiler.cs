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
            var context = new CompilerContext(_image);
            var visitor = new ILAstToCilVisitor(context);
            context.CodeGenerator = visitor;
            
            var methodBody = new CilMethodBody(method);

            // Traverse and recompile the AST.
            methodBody.Instructions.AddRange(unit.AcceptVisitor(visitor));
            
            // Add variables to the method body.
            if (context.Variables.Count > 0)
            {
                methodBody.Signature = new StandAloneSignature(new LocalVariableSignature(context.Variables.Values));
                methodBody.InitLocals = true;
            }
            
            return methodBody;
        }
    }
}