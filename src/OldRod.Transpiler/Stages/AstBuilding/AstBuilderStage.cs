using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.ControlFlow;
using Rivers.Serialization.Dot;

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

            foreach (var entry in context.ControlFlowGraphs)
            {
                uint entryId = context.KoiStream.Exports.First(x => x.Value == entry.Key).Key;
                context.Logger.Debug(Tag, $"Building IL AST for export {entryId}...");
                var unit = builder.BuildAst(entry.Value);
                context.CompilationUnits[entry.Key] = unit;

                if (context.Options.DumpControlFlowGraphs)
                    DumpILAst(context, entryId, unit, entry);
            }
        }

        private static void DumpILAst(DevirtualisationContext context, uint entryId, ILCompilationUnit unit, KeyValuePair<VMExportInfo, ControlFlowGraph> entry)
        {
            context.Logger.Debug(Tag, $"Dumping IL AST for export {entryId}...");
            unit.ControlFlowGraph.UserData["rankdir"] = "LR";

            using (var fs = File.CreateText(Path.Combine(context.Options.OutputDirectory, $"export{entryId}_ilast.dot")))
            {
                var writer = new DotWriter(fs, new BasicBlockSerializer());
                writer.Write(entry.Value);
            }
        }
    }
}