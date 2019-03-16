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
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.Inference;
using Rivers.Serialization.Dot;

namespace OldRod.Pipeline.Stages.VMCodeRecovery
{
    public class VMCodeRecoveryStage : IStage
    {
        public const string Tag = "VMCodeRecovery";
        
        public string Name => "VM code recovery stage";
        
        public void Run(DevirtualisationContext context)
        {
            var disassembler = new InferenceDisassembler(context.TargetImage, context.Constants, context.KoiStream)
            {
                Logger = context.Logger
            };

            foreach (var method in context.VirtualisedMethods)
            {
                context.Logger.Debug(Tag, $"Started VM code recovery of export {method.ExportId}.");
                method.ControlFlowGraph = disassembler.DisassembleExport(method.ExportInfo);
                
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
                        fs.WriteLine("{0,-50} ; {1, -70} {2, -70} {3, -150} {4}",
                            instruction,
                            instruction.InferredMetadata,
                            instruction.ProgramState.Stack,
                            instruction.ProgramState.Registers,
                            "{" + string.Join(", ", instruction.ProgramState.EHStack) + "}");
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
