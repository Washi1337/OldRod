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
            foreach (var method in context.VirtualisedMethods)
            {
                var builder = new ILAstBuilder(context.TargetImage)
                {
                    Logger = context.Logger
                };

                if (context.Options.DumpAllControlFlowGraphs)
                {
                    builder.InitialAstBuilt += (sender, args) =>
                    {
                        context.Logger.Debug(Tag, $"Dumping initial IL AST for export {method.ExportId}...");
                        DumpILAst(context, method, $" (Initial)");
                    };
                    
                    builder.TransformEnd += (sender, transform) =>
                    {
                        context.Logger.Debug(Tag, $"Dumping tentative IL AST for export {method.ExportId}...");
                        DumpILAst(context, method, $" ({transform.Name})");
                    };
                }
                
                context.Logger.Debug(Tag, $"Building IL AST for export {method.ExportId}...");
                var unit = builder.BuildAst(method.ExportInfo.Signature, method.ControlFlowGraph);
                method.ILCompilationUnit = unit;

                if (context.Options.DumpControlFlowGraphs)
                {
                    context.Logger.Log(Tag, $"Dumping final IL AST for export {method.ExportId}...");
                    DumpILAst(context, method);
                }
            }
        }

        private static void DumpILAst(DevirtualisationContext context, VirtualisedMethod method, string suffix = null)
        {
            using (var fs = File.CreateText(Path.Combine(context.Options.OutputDirectory, $"export{method.ExportId}_ilast{suffix}.dot")))
            {
                var writer = new DotWriter(fs, new BasicBlockSerializer());
                writer.Write(Utilities.ConvertToGraphViz(method.ControlFlowGraph, ILAstBlock.AstBlockProperty));
            }
        }
    }
}