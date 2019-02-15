using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.Inference;
using Rivers.Serialization.Dot;

namespace OldRod.Transpiler.Stages.VMCodeRecovery
{
    public class VMCodeRecoveryStage : IStage
    {
        public const string Tag = "VMCode";
        
        public string Name => "VM code recovery stage";
        
        public void Run(DevirtualisationContext context)
        {            
            var infDis = new InferenceDisassembler(context.TargetImage, context.Constants, context.KoiStream);
            infDis.Logger = context.Logger;
            
            context.Logger.Debug(Tag, "Disassembling #Koi stream...");
            var flowGraphs = infDis.BuildFlowGraphs();

            if (context.Options.DumpDisassembledIL)
                DumpDisassembledIL(context, flowGraphs);

            if (context.Options.DumpControlFlowGraphs)
                DumpControlFlowGraphs(context, flowGraphs);

            context.ControlFlowGraphs = flowGraphs;
        }

        private static void DumpDisassembledIL(DevirtualisationContext context, IDictionary<VMExportInfo, ControlFlowGraph> flowGraphs)
        {
            foreach (var entry in flowGraphs)
            {
                uint entryId = context.KoiStream.Exports.First(x => x.Value == entry.Key).Key;
                context.Logger.Log(Tag, $"Dumping IL of export {entryId}...");
                using (var fs = File.CreateText(Path.Combine(context.Options.OutputDirectory, $"export{entryId}_il.koi")))
                {
                    fs.WriteLine("; Function ID: " + entryId);
                    fs.WriteLine("; Function signature: ");
                    fs.WriteLine(";    Flags: 0x{0:X2} (0b{1})", 
                        entry.Key.Signature.Flags,
                        Convert.ToString(entry.Key.Signature.Flags, 2).PadLeft(8, '0'));
                    fs.WriteLine($";    Return Type: {entry.Key.Signature.ReturnToken}");
                    fs.WriteLine($";    Parameter Types: " + string.Join(", ", entry.Key.Signature.ParameterTokens));

                    fs.WriteLine("; Entrypoint Offset: " + entry.Key.CodeOffset.ToString("X4"));
                    fs.WriteLine("; Entrypoint Key: " + entry.Key.EntryKey.ToString("X8"));
                    fs.WriteLine();
                    
                    foreach (var node in entry.Value.Nodes.OrderBy(x => x.Name))
                    {
                        var block = (ILBasicBlock) node.UserData[ILBasicBlock.BasicBlockProperty];
                        foreach (var instruction in block.Instructions)
                        {
                            fs.WriteLine("{0,-50} ; {1, -50} {2, -50} {3}",
                                instruction.ToString(),
                                instruction.ProgramState.Stack.ToString(),
                                instruction.ProgramState.Registers.ToString(),
                                instruction.InferredMetadata);
                        }
                    }
                }
            }
        }

        private static void DumpControlFlowGraphs(DevirtualisationContext context, IDictionary<VMExportInfo, ControlFlowGraph> flowGraphs)
        {
            foreach (var entry in flowGraphs)
            {
                uint entryId = context.KoiStream.Exports.First(x => x.Value == entry.Key).Key;
                context.Logger.Log(Tag, $"Dumping CFG of export {entryId}...");
                using (var fs = File.CreateText(Path.Combine(context.Options.OutputDirectory, $"export{entryId}_il.dot")))
                {
                    var writer = new DotWriter(fs, new BasicBlockSerializer());
                    writer.Write(entry.Value);
                }
            }
        }
    }
}