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
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.DataFlow;
using OldRod.Core.Emulation;

namespace OldRod.Core.Disassembly.Inference
{
    public class InferenceDisassembler
    {
        private const string Tag = "InferenceDisasm";
        
        private readonly MetadataImage _image;
        private readonly VMConstants _constants;
        private readonly KoiStream _koiStream;
        private readonly InstructionProcessor _processor;

        public InferenceDisassembler(MetadataImage image, VMConstants constants, KoiStream koiStream)
        {
            _image = image;
            _constants = constants;
            _koiStream = koiStream ?? throw new ArgumentNullException(nameof(koiStream));
            _processor = new InstructionProcessor(constants, koiStream);
        }

        public ILogger Logger
        {
            get => _processor.Logger;
            set => _processor.Logger = value;
        } 
        
        public ControlFlowGraph DisassembleExport(VMExportInfo export)
        {               
            // TODO: maybe reuse instructions and blockHeaders dictionary for speed up?
            
            var instructions = new Dictionary<long, ILInstruction>();
            var blockHeaders = new HashSet<long>();
                
            // Raw disassemble.
            Logger.Debug(Tag, $"Disassembling instructions...");
            Disassemble(instructions, blockHeaders, export);
                
            // Construct flow graph.
            Logger.Debug(Tag, $"Constructing CFG...");
            return ControlFlowGraphBuilder.BuildGraph(export, instructions.Values, blockHeaders);
        }

        private void Disassemble(IDictionary<long, ILInstruction> visited, ISet<long> blockHeaders, VMExportInfo exportInfo)
        {
            var decoder = new InstructionDecoder(_constants, new MemoryStreamReader(_koiStream.Data)
            {
                Position = exportInfo.CodeOffset
            }, exportInfo.EntryKey);

            var initialState = new ProgramState()
            {
                IP = exportInfo.CodeOffset,
                Key = exportInfo.EntryKey,
            };
            initialState.Stack.Push(new SymbolicValue(new ILInstruction(1, ILOpCodes.CALL, exportInfo.CodeOffset),
                VMType.Qword));
            
            var agenda = new Stack<ProgramState>();
            agenda.Push(initialState);

            while (agenda.Count > 0)
            {
                var currentState = agenda.Pop();
 
                // Check if offset is already visited before.
                if (visited.TryGetValue((long) currentState.IP, out var instruction))
                {
                    // Check if program state is changed, if so, we need to revisit.
                    if (instruction.ProgramState.MergeWith(currentState))
                        currentState = instruction.ProgramState;
                    else
                        continue;
                }
                else
                {
                    // Offset is not visited yet, read instruction. 
                    decoder.Reader.Position = (long) currentState.IP;
                    decoder.CurrentKey = currentState.Key;
                    
                    instruction = decoder.ReadNextInstruction();
                    
                    instruction.ProgramState = currentState;
                    visited.Add((long) currentState.IP, instruction);
                }

                currentState.Registers[VMRegisters.IP] = new SymbolicValue(instruction, VMType.Qword);
                
                // Determine next states.
                foreach (var state in _processor.GetNextStates(blockHeaders, currentState, instruction, decoder))
                    agenda.Push(state);
            }
        }

       
    }
}