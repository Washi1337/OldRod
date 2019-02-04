using System;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using OldRod.Core.Ast;
using OldRod.Core.Recompiler;

namespace OldRod.Transpiler.Stages.AstBuilding
{
    public class AstBuilderStage : IStage
    {
        public const string Tag = "AstBuilder";
        
        public string Name => "IL AST builder stage";

        public void Run(DevirtualisationContext context)
        {
            var builder = new ILAstBuilder(context.TargetImage);
            var unit = builder.BuildAst(context.DisassembledInstructions, context.KoiStream.Exports[3].CodeOffset);

            Console.WriteLine("Variables: ");
            foreach (var variable in unit.GetVariables())
                Console.WriteLine("- " + variable.VariableType + " " + variable.Name);

            Console.WriteLine("Code: ");
            foreach (var statement in unit.Statements)
                Console.WriteLine(statement.ToString());

            var recompiler = new ILAstCompiler(context.TargetImage);

            var targetMethod = (MethodDefinition) context.TargetImage.ResolveMember(new MetadataToken(MetadataTokenType.Method, 3));
            var newBody = recompiler.Compile(targetMethod, unit);
            targetMethod.CilMethodBody = newBody;
            
            Console.WriteLine("Recompiled code:");
            foreach (var instruction in newBody.Instructions) 
                Console.WriteLine(instruction);
        }
    }
}