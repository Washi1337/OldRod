using System;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;

namespace OldRod.Core.CodeGen
{
    public class CilMethodBodyGenerator
    {
        private readonly VMConstants _constants;
        private readonly TypeDefinition _flagHelperType;

        public CilMethodBodyGenerator(VMConstants constants, TypeDefinition flagHelperType)
        {
            _constants = constants ?? throw new ArgumentNullException(nameof(constants));
            _flagHelperType = flagHelperType ?? throw new ArgumentNullException(nameof(flagHelperType));
        }

        public CilMethodBody Compile(MethodDefinition method, CilCompilationUnit unit)
        {
            var methodBody = new CilMethodBody(method);
            
            var context = new CodeGenerationContext(methodBody, _constants, unit.FlagVariable, _flagHelperType);
            var visitor = new CilCodeGenerator(context);
            context.CodeGenerator = visitor;
            
            // Traverse and recompile the AST.
            methodBody.Instructions.AddRange(unit.AcceptVisitor(visitor));
            
            // Add variables to the method body.
            if (context.Variables.Count > 0)
            {
                methodBody.Signature = new StandAloneSignature(new LocalVariableSignature(context.Variables));
                methodBody.InitLocals = true;
            }
            
            return methodBody;
        }
    }
}