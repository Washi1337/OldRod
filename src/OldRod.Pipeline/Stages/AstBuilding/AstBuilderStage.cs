using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
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
                int step = 0;
                
                var builder = new ILAstBuilder(context.TargetImage)
                {
                    Logger = context.Logger
                };

                if (context.Options.OutputOptions.DumpAllControlFlowGraphs)
                {
                    builder.InitialAstBuilt += (sender, args) =>
                    {
                        context.Logger.Debug(Tag, $"Dumping initial IL AST for export {method.ExportId}...");
                        DumpILAst(context, method, $" (0. Initial)");
                    };
                    
                    builder.TransformEnd += (sender, args) =>
                    {
                        step++;
                        context.Logger.Debug(Tag, $"Dumping tentative IL AST for export {method.ExportId}...");
                        DumpILAst(context, method, $" ({step}. {args.Transform.Name}-{args.Iteration})");
                    };
                }
                
                context.Logger.Debug(Tag, $"Building IL AST for export {method.ExportId}...");
                var unit = builder.BuildAst(method.ExportInfo.Signature, method.ControlFlowGraph);
                method.ILCompilationUnit = unit;

                if (context.Options.OutputOptions.DumpControlFlowGraphs)
                {
                    context.Logger.Log(Tag, $"Dumping IL AST for export {method.ExportId}...");
                    DumpILAst(context, method);
                    
                    using (var fs = File.CreateText(Path.Combine(context.Options.OutputOptions.ILAstDumpsDirectory, $"export{method.ExportId}_tree.dot")))
                    {
                        var writer = new DotWriter(fs, new BasicBlockSerializer());
                        writer.Write(method.ILCompilationUnit.ConvertToGraphViz(method.CallerMethod));
                    }
                }
            }
        }

        private static void DumpILAst(DevirtualisationContext context, VirtualisedMethod method, string suffix = null)
        {
            using (var fs = File.CreateText(Path.Combine(context.Options.OutputOptions.ILAstDumpsDirectory, $"export{method.ExportId}{suffix}.dot")))
            {
                var writer = new DotWriter(fs, new BasicBlockSerializer());
                writer.Write(method.ControlFlowGraph.ConvertToGraphViz(ILAstBlock.AstBlockProperty));
            }
        }
        
    }
}