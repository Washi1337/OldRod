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
using System.Linq;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using OldRod.Core.CodeGen;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Recompiler;
using Rivers.Serialization.Dot;

namespace OldRod.Pipeline.Stages.Recompiling
{
    public class RecompilerStage : IStage
    {
        public const string Tag = "Recompiler";
        
        public string Name => "CIL Recompiler stage";

        public void Run(DevirtualisationContext context)
        {
            var flagHelper = VmHelperGenerator.ImportFlagHelper(context.TargetImage, context.Constants);
            foreach (var method in context.VirtualisedMethods)
            {
                // Recompile IL AST to CIL AST.
                context.Logger.Debug(Tag, $"Recompiling function_{method.Function.EntrypointAddress:X4} to CIL-AST...");
                
                var recompiler = new ILToCilRecompiler(method.CallerMethod.CilMethodBody, context.TargetImage, context);
                method.CilCompilationUnit = recompiler.Recompile(method.ILCompilationUnit);
                if (context.Options.OutputOptions.DumpControlFlowGraphs)
                {
                    context.Logger.Log(Tag, $"Dumping CIL Ast of function_{method.Function.EntrypointAddress:X4}...");
                    DumpCilAst(context, method);
                }
             
                // Generate final CIL code.
                context.Logger.Debug(Tag, $"Generating CIL for function_{method.Function.EntrypointAddress:X4}...");
                
                var generator = new CilMethodBodyGenerator(context.Constants, flagHelper);
                method.CallerMethod.CilMethodBody = generator.Compile(method.CallerMethod, method.CilCompilationUnit);
                if (context.Options.OutputOptions.DumpRecompiledCil)
                {
                    context.Logger.Log(Tag, $"Dumping CIL of function_{method.Function.EntrypointAddress:X4}...");
                    DumpCil(context, method);
                }
            }
        }

        private static void DumpCilAst(DevirtualisationContext context, VirtualisedMethod method)
        {
            using (var fs = File.CreateText(Path.Combine(context.Options.OutputOptions.CilAstDumpsDirectory, $"function_{method.Function.EntrypointAddress:X4}.dot")))
            {
                var writer = new DotWriter(fs, new BasicBlockSerializer(method.CallerMethod.CilMethodBody));
                writer.Write(method.ControlFlowGraph.ConvertToGraphViz(CilAstBlock.AstBlockProperty));
            }
            
            using (var fs = File.CreateText(Path.Combine(context.Options.OutputOptions.CilAstDumpsDirectory, $"function_{method.Function.EntrypointAddress:X4}_tree.dot")))
            {
                var writer = new DotWriter(fs, new BasicBlockSerializer());
                writer.Write(method.CilCompilationUnit.ConvertToGraphViz(method.CallerMethod));
            }
        }

        private static void DumpCil(DevirtualisationContext context, VirtualisedMethod method)
        {
            var formatter = new CilInstructionFormatter(method.CallerMethod.CilMethodBody);
            using (var fs = File.CreateText(Path.Combine(context.Options.OutputOptions.CilDumpsDirectory, $"function_{method.Function.EntrypointAddress:X4}.il")))
            {
                foreach (var instruction in method.CallerMethod.CilMethodBody.Instructions)
                    fs.WriteLine(formatter.FormatInstruction(instruction));
            }
        }
        
        
    }
}