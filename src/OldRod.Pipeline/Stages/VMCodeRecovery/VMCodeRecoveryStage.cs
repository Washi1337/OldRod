using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.Inference;
using Rivers.Serialization.Dot;

namespace OldRod.Pipeline.Stages.VMCodeRecovery
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

            foreach (var entry in flowGraphs)
            {
                foreach (var method in context.VirtualisedMethods)
                {
                    if (entry.Key == method.ExportInfo)
                    {
                        method.ControlFlowGraph = entry.Value;
                        if (context.Options.OutputOptions.DumpDisassembledIL)
                        {
                            context.Logger.Log(Tag, $"Dumping IL of export {method.ExportId}...");
                            DumpDisassembledIL(context, method);
                        }

                        if (context.Options.OutputOptions.DumpControlFlowGraphs)
                        {
                            context.Logger.Log(Tag, $"Dumping CFG of export {method.ExportId}...");
                            DumpControlFlowGraph(context, method);
                        }
                    }
                }
            }
        }

        private static void DumpDisassembledIL(DevirtualisationContext context, VirtualisedMethod method)
        {
            var exportInfo = method.ExportInfo;

            using (var fs = File.CreateText(Path.Combine(
                context.Options.OutputOptions.ILDumpsDirectory, 
                $"export{method.ExportId}.koi")))
            {
                // Write basic information about export:
                fs.WriteLine("; Function ID: " + method.ExportId);
                fs.WriteLine("; Raw function signature: ");
                fs.WriteLine(";    Flags: 0x{0:X2} (0b{1})",
                    exportInfo.Signature.Flags,
                    Convert.ToString(exportInfo.Signature.Flags, 2).PadLeft(8, '0'));
                fs.WriteLine($";    Return Type: {exportInfo.Signature.ReturnToken}");
                fs.WriteLine($";    Parameter Types: " + string.Join(", ", exportInfo.Signature.ParameterTokens));
                fs.WriteLine("; Converted method signature: " + method.ConvertedMethodSignature);
                fs.WriteLine("; Physical method: " + method.CallerMethod);
                fs.WriteLine("; Entrypoint Offset: " + exportInfo.CodeOffset.ToString("X4"));
                fs.WriteLine("; Entrypoint Key: " + exportInfo.EntryKey.ToString("X8"));
                
                fs.WriteLine();

                // Write contents of nodes.
                foreach (var node in method.ControlFlowGraph.Nodes.OrderBy(x => x.Name))
                {
                    var block = (ILBasicBlock) node.UserData[ILBasicBlock.BasicBlockProperty];
                    foreach (var instruction in block.Instructions)
                    {
                        fs.WriteLine("{0,-50} ; {1, -70} {2, -150} {3}",
                            instruction,
                            instruction.ProgramState.Stack,
                            instruction.ProgramState.Registers,
                            instruction.InferredMetadata);
                    }
                }
            }
        }

        private static void DumpControlFlowGraph(DevirtualisationContext context, VirtualisedMethod method)
        {
            using (var fs = File.CreateText(Path.Combine(
                    context.Options.OutputOptions.ILDumpsDirectory, 
                    $"export{method.ExportId}.dot")))
            {
                var writer = new DotWriter(fs, new BasicBlockSerializer());
                writer.Write(method.ControlFlowGraph.ConvertToGraphViz(ILBasicBlock.BasicBlockProperty));
            }
        }
    }
}
