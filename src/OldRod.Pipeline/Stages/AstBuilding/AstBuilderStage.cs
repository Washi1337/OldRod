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

using System;
using System.IO;
using System.Linq;
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
                if (method.IsExport
                    && !context.Options.SelectedExports.Contains(method.ExportId.Value, method.ExportInfo))
                    continue;
                    
                context.Logger.Debug(Tag, $"Building IL AST for function_{method.Function.EntrypointAddress:X4}...");

                // Create builder.
                var builder = new ILAstBuilder(context.TargetImage)
                {
                    Logger = context.Logger
                };

                // Subscribe to progress events if user specified it in the options. 
                if (context.Options.OutputOptions.DumpAllControlFlowGraphs)
                {
                    int step = 1;
                    builder.InitialAstBuilt += (sender, args) =>
                    {
                        context.Logger.Debug(Tag, $"Dumping initial IL AST for function_{method.Function.EntrypointAddress:X4}...");
                        method.ILCompilationUnit = args;
                        DumpILAst(context, method, $" (0. Initial)");
                    };

                    builder.TransformEnd += (sender, args) =>
                    {
                        context.Logger.Debug(Tag,$"Dumping tentative IL AST for function_{method.Function.EntrypointAddress:X4}...");
                        method.ILCompilationUnit = args.Unit;
                        DumpILAst(context, method, $" ({step++}. {args.Transform.Name}-{args.Iteration})");
                    };
                }

                // Build the AST.
                method.ILCompilationUnit = builder.BuildAst(method.ControlFlowGraph, method.Function.FrameLayout, context.Constants);

                // Dump graphs if user specified it in the options.
                if (context.Options.OutputOptions.DumpControlFlowGraphs)
                {
                    context.Logger.Log(Tag, $"Dumping IL AST for function_{method.Function.EntrypointAddress:X4}...");
                    DumpILAst(context, method);

                    DumpILAstTree(context, method);
                }
            }
        }

        private static void DumpILAstTree(DevirtualisationContext context, VirtualisedMethod method)
        {
            using (var fs = File.CreateText(Path.Combine(context.Options.OutputOptions.ILAstDumpsDirectory,
                $"function_{method.Function.EntrypointAddress:X4}_tree.dot")))
            {
                WriteHeader(fs, method);
                var writer = new DotWriter(fs, new BasicBlockSerializer());
                writer.Write(method.ILCompilationUnit.ConvertToGraphViz(method.CallerMethod));
            }
        }

        private static void DumpILAst(DevirtualisationContext context, VirtualisedMethod method, string suffix = null)
        {
            using (var fs = File.CreateText(Path.Combine(
                context.Options.OutputOptions.ILAstDumpsDirectory, 
                $"function_{method.Function.EntrypointAddress:X4}{suffix}.dot")))
            {
                WriteHeader(fs, method);
                var writer = new DotWriter(fs, new BasicBlockSerializer());
                writer.Write(method.ControlFlowGraph.ConvertToGraphViz(ILAstBlock.AstBlockProperty));
            }
        }

        private static void WriteHeader(TextWriter writer, VirtualisedMethod method)
        {   
            writer.WriteLine("// Export ID: " + (method.ExportId?.ToString() ?? "<none>"));

            var exportInfo = method.ExportInfo;
            if (exportInfo != null)
            {
                writer.WriteLine("// Raw function signature: ");
                writer.WriteLine("//    Flags: 0x{0:X2} (0b{1})",
                    exportInfo.Signature.Flags,
                    Convert.ToString(exportInfo.Signature.Flags, 2).PadLeft(8, '0'));
                writer.WriteLine($"//    Return Type: {exportInfo.Signature.ReturnToken}");
                writer.WriteLine($"//    Parameter Types: " + string.Join(", ", exportInfo.Signature.ParameterTokens));
            }

            writer.WriteLine("// Inferred method signature: " + method.ConvertedMethodSignature);
            writer.WriteLine("// Physical method: " + method.CallerMethod);
            writer.WriteLine("// Entrypoint Offset: " + method.Function.EntrypointAddress.ToString("X4"));
            writer.WriteLine("// Entrypoint Key: " + method.Function.EntryKey.ToString("X8"));
                
            writer.WriteLine();
            
            writer.WriteLine("// Variables: ");
            
            foreach (var variable in method.ILCompilationUnit.Variables)
                writer.WriteLine($"//    {variable.Name}: {variable.VariableType} (assigned {variable.AssignedBy.Count}x, used {variable.UsedBy.Count}x)");
            
            writer.WriteLine();
        }
    }
}