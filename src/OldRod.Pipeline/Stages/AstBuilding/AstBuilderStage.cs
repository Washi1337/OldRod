using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.ControlFlow;
using Rivers.Serialization.Dot;

namespace OldRod.Pipeline.Stages.AstBuilding
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

            foreach (var method in context.VirtualisedMethods)
            {
                context.Logger.Debug(Tag, $"Building IL AST for export {method.ExportId}...");
                var unit = builder.BuildAst(method.ControlFlowGraph);
                method.ILCompilationUnit = unit;

                if (context.Options.DumpControlFlowGraphs)
                {
                    context.Logger.Log(Tag, $"Dumping IL AST for export {method.ExportId}...");
                    DumpILAst(context, method);
                }
            }
        }

        private static void DumpILAst(DevirtualisationContext context, VirtualisedMethod method)
        {
            using (var fs = File.CreateText(Path.Combine(context.Options.OutputDirectory, $"export{method.ExportId}_ilast.dot")))
            {
                var writer = new DotWriter(fs, new BasicBlockSerializer());
                writer.Write(Utilities.ConvertToGraphViz(method.ControlFlowGraph, ILAstBlock.AstBlockProperty));
            }
        }
    }
}