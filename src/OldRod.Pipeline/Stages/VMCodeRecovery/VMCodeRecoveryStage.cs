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
                Logger = context.Logger,
                FunctionFactory = new ExportsAwareFunctionFactory(context),
                SalvageCfgOnError = context.Options.EnableSalvageMode,
                ExitKeyResolver = new SimpleExitKeyBruteForce(),
                ResolveUnknownExitKeys = true
            };

            // Register functions entry points.
            foreach (var method in context.VirtualisedMethods)
            {
                if (!method.ExportInfo.IsSignatureOnly
                    && (!method.IsExport || context.Options.SelectedExports.Contains(method.ExportId.Value, method.ExportInfo)))
                {
                    disassembler.AddFunction(method.Function);
                }
            }

            // Listen for new explored functions.
            var newFunctions = new Dictionary<uint, VMFunction>();
            disassembler.FunctionInferred += (sender, args) =>
            {
                var method = context.ResolveMethod(args.Function.EntrypointAddress);
                if (method == null)
                    newFunctions.Add(args.Function.EntrypointAddress, args.Function);
            };

            // Disassemble!
            var controlFlowGraphs = disassembler.DisassembleFunctions();
                
            foreach (var entry in controlFlowGraphs)
            {
                VirtualisedMethod method;
                if (newFunctions.ContainsKey(entry.Key))
                {
                    context.Logger.Debug(Tag, $"Creating method for function_{entry.Key:X4}.");
                    method = new VirtualisedMethod(newFunctions[entry.Key]);
                    context.VirtualisedMethods.Add(method);
                }
                else
                {
                    method = context.VirtualisedMethods.First(x => x.Function.EntrypointAddress == entry.Key);
                }

                method.ControlFlowGraph = entry.Value;
                
                if (context.Options.OutputOptions.DumpDisassembledIL)
                {
                    context.Logger.Log(Tag, $"Dumping IL of function_{entry.Key:X4}...");
                    DumpDisassembledIL(context, method);
                }

                if (context.Options.OutputOptions.DumpControlFlowGraphs)
                {
                    context.Logger.Log(Tag, $"Dumping CFG of function_{entry.Key:X4}...");
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

                fs.WriteLine("; Inferred method signature: " + method.MethodSignature);
                fs.WriteLine("; Physical method: " + method.CallerMethod);
                fs.WriteLine("; Entrypoint Offset: " + method.Function.EntrypointAddress.ToString("X4"));
                fs.WriteLine("; Entrypoint Key: " + method.Function.EntryKey.ToString("X8"));
                
                fs.WriteLine();

                // Write contents of nodes.
                int instructionLength = -1;
                int annotationLength = -1;
                int stackLength = -1;
                int registersLength = -1;
                
                foreach (var node in method.ControlFlowGraph.Nodes)
                {
                    node.UserData.TryGetValue(ILBasicBlock.BasicBlockProperty, out var b);
                    if (b != null)
                    {
                        var block = (ILBasicBlock) b;
                        foreach (var instruction in block.Instructions)
                        {
                            instructionLength = Math.Max(instruction.ToString().Length, instructionLength);
                            annotationLength = Math.Max(instruction.Annotation?.ToString().Length ?? 0, annotationLength);
                            stackLength = Math.Max(instruction.ProgramState.Stack.ToString().Length, stackLength);
                            registersLength = Math.Max(instruction.ProgramState.Registers.ToString().Length, registersLength);
                        }
                    }
                }

                const int separatorLength = 3;
                instructionLength += separatorLength;
                annotationLength += separatorLength;
                stackLength += separatorLength;
                registersLength += separatorLength;
                
                fs.Write("; Instruction".PadRight(instructionLength));
                fs.Write("   ");
                fs.Write("Annotation".PadRight(annotationLength));
                fs.Write(" ");
                fs.Write("Stack".PadRight(stackLength));
                fs.Write(" ");
                fs.Write("Registers".PadRight(registersLength));
                fs.WriteLine(" EH stack");

                foreach (var node in method.ControlFlowGraph.Nodes.OrderBy(x => x.Name))
                {
                    node.UserData.TryGetValue(ILBasicBlock.BasicBlockProperty, out var b);
                    if (b == null)
                    {
                        fs.WriteLine("; <<< unknown >>> ");
                        fs.WriteLine();
                    }
                    else
                    {
                        var block = (ILBasicBlock) b;
                        foreach (var instruction in block.Instructions)
                        {
                            fs.Write(instruction.ToString().PadRight(instructionLength));
                            fs.Write(" ; ");
                            fs.Write((instruction.Annotation?.ToString() ?? string.Empty).PadRight(annotationLength));
                            fs.Write(" ");
                            fs.Write(instruction.ProgramState.Stack.ToString().PadRight(stackLength));
                            fs.Write(" ");
                            fs.Write(instruction.ProgramState.Registers.ToString().PadRight(registersLength));
                            fs.Write(" ");
                            fs.WriteLine("{" + string.Join(", ", instruction.ProgramState.EHStack) + "}");
                        }

                        fs.WriteLine();
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
