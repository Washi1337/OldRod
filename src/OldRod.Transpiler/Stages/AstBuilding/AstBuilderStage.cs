using OldRod.Core.Ast;

namespace OldRod.Transpiler.Stages.AstBuilding
{
    public class AstBuilderStage : IStage
    {
        public const string Tag = "AstBuilder";
        
        public string Name => "IL AST builder stage";

        public void Run(DevirtualisationContext context)
        {
            var builder = new ILAstBuilder(context.TargetImage)
            {
                Logger = context.Logger
            };

            foreach (var graph in context.ControlFlowGraphs)
            {
                var unit = builder.BuildAst(graph.Value);
                context.CompilationUnits[graph.Key] = unit;
            }
        }
    }
}