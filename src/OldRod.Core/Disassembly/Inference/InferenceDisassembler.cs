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
using System.Linq;
using AsmResolver;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.DataFlow;

namespace OldRod.Core.Disassembly.Inference
{
    public class InferenceDisassembler
    {
        private const string Tag = "InferenceDisasm";
        
        private readonly VMConstants _constants;
        private readonly KoiStream _koiStream;
        private readonly InstructionDecoder _decoder;
        private readonly InstructionProcessor _processor;

        public InferenceDisassembler(VMConstants constants, KoiStream koiStream)
        {
            _constants = constants;
            _koiStream = koiStream ?? throw new ArgumentNullException(nameof(koiStream));
            _decoder = new InstructionDecoder(_constants, new MemoryStreamReader(_koiStream.Data));
            _processor = new InstructionProcessor(constants, koiStream);
        }

        public ILogger Logger
        {
            get => _processor.Logger;
            set => _processor.Logger = value;
        }

        public IDictionary<uint, ControlFlowGraph> DisassembleExports()
        {
            var disassemblies = _koiStream.Exports.ToDictionary(
                x => x.Key,
                x => new VMExportDisassembly(x.Value));
            
            bool changed = true;

            while (changed)
            {
                changed = false;
                
                foreach (var entry in disassemblies)
                {
                    var disassembly = entry.Value;

                    var initialStates = new List<ProgramState>();
                    if (disassembly.Instructions.Count == 0)
                    {
                        // First run. We just start at the very beginning of the export.
                        Logger.Debug(Tag, $"Started disassembling export {entry.Key}...");
                        var initialState = new ProgramState()
                        {
                            IP = disassembly.ExportInfo.CodeOffset,
                            Key = disassembly.ExportInfo.EntryKey,
                        };
                        initialState.Stack.Push(new SymbolicValue(new ILInstruction(1, ILOpCodes.CALL, disassembly.ExportInfo.CodeOffset),
                            VMType.Qword));
                        initialStates.Add(initialState);
                        disassembly.BlockHeaders.Add(disassembly.ExportInfo.CodeOffset);
                    }
                    else if (disassembly.UnresolvedOffsets.Count > 0)
                    {
                        // We still have some instructions were we could not fully resolve the next program states from.
                        // Currently the only reason for this to happen is when we disassembled a CALL instruction, and
                        // inferred that it called a method whose exit key was not yet known, and could therefore not
                        // continue disassembly.
                        
                        // Continue disassembly at this position:
                        Logger.Debug(Tag, $"Revisiting unresolved offsets for export {entry.Key}...");
                        foreach (long offset in disassembly.UnresolvedOffsets)
                            initialStates.Add(disassembly.Instructions[offset].ProgramState);
                    }

                    if (initialStates.Count > 0)
                    {
                        bool entryChanged = ContinueDisassembly(disassembly, initialStates);

                        if (disassembly.UnresolvedOffsets.Count > 0)
                        {
                            Logger.Debug(Tag,
                                $"Disassembly procedure stopped with {disassembly.UnresolvedOffsets.Count} unresolved offsets (new instructions decoded: {entryChanged}).");
                        }
                        else if (disassembly.Instructions.Count == 0)
                        {
                            Logger.Warning(Tag,
                                $"Disassembly finalised with {disassembly.Instructions.Count} instructions.");
                        }
                        else
                        {   
                            Logger.Debug(Tag,
                                $"Disassembly finalised with {disassembly.Instructions.Count} instructions.");
                        }

                        changed |= entryChanged;
                    }
                }
            }

            var result = new Dictionary<uint, ControlFlowGraph>();
            foreach (var entry in disassemblies)
            {
                if (entry.Value.UnresolvedOffsets.Count > 0)
                {
                    Logger.Warning(Tag,string.Format("Could not resolve the next states of some offsets of export {0} ({1}).",
                        entry.Key,
                        string.Join(", ", entry.Value.UnresolvedOffsets.Select(x => "IL_" + x.ToString("X4")))));
                }

                Logger.Debug(Tag, $"Constructing CFG of export {entry.Key}...");
                result[entry.Key] = ControlFlowGraphBuilder.BuildGraph(entry.Value);
            }

            return result;
        }

        private bool ContinueDisassembly(VMExportDisassembly disassembly, IEnumerable<ProgramState> initialStates)
        {
            bool changed = false;
            
            var agenda = new Stack<ProgramState>();
            var initials = new HashSet<ulong>();
            foreach (var state in initialStates)
            {
                initials.Add(state.IP);
                agenda.Push(state);
            }

            while (agenda.Count > 0)
            {
                var currentState = agenda.Pop();

                // Check if offset is already visited before.
                if (disassembly.Instructions.TryGetValue((long) currentState.IP, out var instruction))
                {
                    // Check if program state is changed, if so, we need to revisit.
                    if (instruction.ProgramState.MergeWith(currentState) || initials.Contains(currentState.IP))
                        currentState = instruction.ProgramState;
                    else
                        continue;
                }
                else
                {
                    // Offset is not visited yet, read instruction. 
                    _decoder.Reader.Position = (long) currentState.IP;
                    _decoder.CurrentKey = currentState.Key;

                    instruction = _decoder.ReadNextInstruction();

                    instruction.ProgramState = currentState;
                    disassembly.Instructions.Add((long) currentState.IP, instruction);
                    changed = true;
                }

                currentState.Registers[VMRegisters.IP] = new SymbolicValue(instruction, VMType.Qword);

                // Determine next states.
                foreach (var state in _processor.GetNextStates(disassembly, currentState, instruction, _decoder.CurrentKey))
                    agenda.Push(state);
            }

            return changed;
        }

    }
}