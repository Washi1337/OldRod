using System;
using OldRod.Core.Ast;

namespace OldRod.Transpiler.Stages.AstBuilding
{
    public class AstBuilderStage : IStage
    {
        public const string Tag = "AstBuilder";
        
        public string Name => "IL AST builder stage";

        public void Run(DevirtualisationContext context)
        {
            var builder = new ILAstBuilder();
            var unit = builder.BuildAst(context.DisassembledInstructions, context.KoiStream.Exports[4].CodeOffset);

            foreach (var statement in unit.Statements)
                Console.WriteLine(statement.ToString());
        }
    }
}