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
using System.Collections.Generic;
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
            var disassembler = new InferenceDisassembler(context.Constants, context.KoiStream)
            {
                Logger = context.Logger
            };

            // Register functions entry points.
            foreach (var method in context.VirtualisedMethods)
                disassembler.AddFunction(method.Function);

            // Listen for new explored functions.
            var newFunctions = new HashSet<VMFunction>();
            disassembler.FunctionInferred += (sender, args) => newFunctions.Add(args.Function);

            // Disassemble!
            var controlFlowGraphs = disassembler.DisassembleExports();
                
            foreach (var entry in controlFlowGraphs)
            {
                // TODO: do something with the newly inferred functions.
                
                var method = context.VirtualisedMethods.First(x => x.Function.EntrypointAddress == entry.Key);
                method.ControlFlowGraph = entry.Value;
                
                if (context.Options.OutputOptions.DumpDisassembledIL)
                {
                    context.Logger.Log(Tag, $"Dumping IL of function_{method.Function.EntrypointAddress:X4}...");
                    DumpDisassembledIL(context, method);
                }

                if (context.Options.OutputOptions.DumpControlFlowGraphs)
                {
                    context.Logger.Log(Tag, $"Dumping CFG of function_{method.Function.EntrypointAddress:X4}...");
                    DumpControlFlowGraph(context, method);
                }
            }
        }

        private static void DumpDisassembledIL(DevirtualisationContext context, VirtualisedMethod method)
        {

            using (var fs = File.CreateText(Path.Combine(
                context.Options.OutputOptions.ILDumpsDirectory, 
                $"function_{method.Function.EntrypointAddress:X4}.koi")))
            {
                // Write basic information about export:
                fs.WriteLine("; Export ID: " + (method.ExportId?.ToString() ?? "<none>"));

                var exportInfo = method.ExportInfo;
                if (exportInfo != null)
                {
                    fs.WriteLine("; Raw function signature: ");
                    fs.WriteLine(";    Flags: 0x{0:X2} (0b{1})",
                        exportInfo.Signature.Flags,
                        Convert.ToString(exportInfo.Signature.Flags, 2).PadLeft(8, '0'));
                    fs.WriteLine($";    Return Type: {exportInfo.Signature.ReturnToken}");
                    fs.WriteLine($";    Parameter Types: " + string.Join(", ", exportInfo.Signature.ParameterTokens));
                }

                fs.WriteLine("; Inferred method signature: " + method.ConvertedMethodSignature);
                fs.WriteLine("; Physical method: " + method.CallerMethod);
                fs.WriteLine("; Entrypoint Offset: " + method.Function.EntrypointAddress.ToString("X4"));
                fs.WriteLine("; Entrypoint Key: " + method.Function.EntryKey.ToString("X8"));
                
                fs.WriteLine();

                // Write contents of nodes.
                foreach (var node in method.ControlFlowGraph.Nodes.OrderBy(x => x.Name))
                {
                    var block = (ILBasicBlock) node.UserData[ILBasicBlock.BasicBlockProperty];
                    foreach (var instruction in block.Instructions)
                    {
                        fs.WriteLine("{0,-50} ; {1, -70} {2, -70} {3, -150} {4}",
                            instruction,
                            instruction.Annotation,
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
                    $"function_{method.Function.EntrypointAddress:X4}.dot")))
            {
                var writer = new DotWriter(fs, new BasicBlockSerializer());
                writer.Write(method.ControlFlowGraph.ConvertToGraphViz(ILBasicBlock.BasicBlockProperty));
            }
        }
    }
}
