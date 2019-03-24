// Project OldRod - A KoiVM devirtualisation utility.
// Copyright (C) 2019 Washi
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

using System.IO;
using OldRod.Core.Ast.IL;
using OldRod.Core.Memory;
using Rivers.Serialization.Dot;

namespace OldRod.Pipeline.Stages.AstBuilding
{
    public class AstBuilderStage : IStage
    {
        private const string Tag = "AstBuilder";
        
        public string Name => "IL AST builder stage";

        public void Run(DevirtualisationContext context)
        {
            foreach (var method in context.VirtualisedMethods)
            {
                context.Logger.Debug(Tag, $"Building IL AST for function_{method.Function.EntrypointAddress:X4}...");
                
                // Create builder.
                var builder = new ILAstBuilder(context.TargetImage)
                {
                    Logger = context.Logger
                };

                // Subscribe to progress events if user specified it in the options. 
                if (context.Options.OutputOptions.DumpAllControlFlowGraphs)
                {
                    int step = 0;
                    builder.InitialAstBuilt += (sender, args) =>
                    {
                        context.Logger.Debug(Tag, $"Dumping initial IL AST for function_{method.Function.EntrypointAddress:X4}...");
                        DumpILAst(context, method, $" (0. Initial)");
                    };
                    
                    builder.TransformEnd += (sender, args) =>
                    {
                        step++;
                        context.Logger.Debug(Tag, $"Dumping tentative IL AST for function_{method.Function.EntrypointAddress:X4}...");
                        DumpILAst(context, method, $" ({step}. {args.Transform.Name}-{args.Iteration})");
                    };
                }

                // Build the AST.
                method.ILCompilationUnit = builder.BuildAst(method.ControlFlowGraph, method.FrameLayout);

                // Dump graphs if user specified it in the options.
                if (context.Options.OutputOptions.DumpControlFlowGraphs)
                {
                    context.Logger.Log(Tag, $"Dumping IL AST for function_{method.Function.EntrypointAddress:X4}...");
                    DumpILAst(context, method);
                    
                    using (var fs = File.CreateText(Path.Combine(context.Options.OutputOptions.ILAstDumpsDirectory, $"function_{method.Function.EntrypointAddress:X4}_tree.dot")))
                    {
                        var writer = new DotWriter(fs, new BasicBlockSerializer());
                        writer.Write(method.ILCompilationUnit.ConvertToGraphViz(method.CallerMethod));
                    }
                }
            }
        }

        private static void DumpILAst(DevirtualisationContext context, VirtualisedMethod method, string suffix = null)
        {
            using (var fs = File.CreateText(Path.Combine(
                context.Options.OutputOptions.ILAstDumpsDirectory, 
                $"function_{method.Function.EntrypointAddress:X4}{suffix}.dot")))
            {
                var writer = new DotWriter(fs, new BasicBlockSerializer());
                writer.Write(method.ControlFlowGraph.ConvertToGraphViz(ILAstBlock.AstBlockProperty));
            }
        }
        
    }
}