using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.DataFlow;
using OldRod.Core.Emulation;
using Rivers;

namespace OldRod.Core.Disassembly.Inference
{
    public class InferenceDisassembler
    {
        private const string Tag = "InferenceDisassembler";
        
        private readonly MetadataImage _image;
        private readonly VMConstants _constants;
        private readonly KoiStream _koiStream;
        private readonly VCallProcessor _vCallProcessor;

        public InferenceDisassembler(MetadataImage image, VMConstants constants, KoiStream koiStream)
        {
            _image = image;
            _constants = constants;
            _koiStream = koiStream ?? throw new ArgumentNullException(nameof(koiStream));
            _vCallProcessor = new VCallProcessor(image, _constants, _koiStream);
        }

        public ILogger Logger
        {
            get;
            set;
        } = EmptyLogger.Instance;
        
        public IDictionary<VMExportInfo, ControlFlowGraph> BuildFlowGraphs()
        {
            // TODO: maybe reuse instructions and blockHeaders dictionary for speed up?
            
            var result = new Dictionary<VMExportInfo, ControlFlowGraph>();
            foreach (var entry in _koiStream.Exports)
            {
                var export = entry.Value;
                var instructions = new Dictionary<long, ILInstruction>();
                var blockHeaders = new HashSet<long>();
                
                // Raw disassemble.
                Logger.Debug(Tag, "Disassembling export " + entry.Key + "...");
                Disassemble(instructions, blockHeaders, export);
                
                // Construct flow graph.
                Logger.Debug(Tag, "Building CFG for export " + entry.Key + "...");
                var graph = BuildGraph(export, instructions, blockHeaders);
                result.Add(export, graph);
            }

            return result;
        }

        private void Disassemble(IDictionary<long, ILInstruction> visited, ISet<long> blockHeaders, VMExportInfo exportInfo)
        {
            var disassembler = new LinearDisassembler(_constants, new MemoryStreamReader(_koiStream.Data)
            {
                Position = exportInfo.CodeOffset
            }, exportInfo.EntryKey);

            var initialState = new ProgramState() {IP = exportInfo.CodeOffset};
            
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
                    disassembler.Reader.Position = (long) currentState.IP;
                    instruction = disassembler.ReadNextInstruction();
                    instruction.ProgramState = currentState;
                    currentState.Registers[VMRegisters.IP] = new SymbolicValue(instruction);
                    visited.Add((long) currentState.IP, instruction);
                }

                // Determine next states.
                foreach (var state in GetNextStates(blockHeaders, currentState, instruction))
                    agenda.Push(state);
            }
        }

        private IList<ProgramState> GetNextStates(ISet<long> blockHeaders, ProgramState currentState, ILInstruction instruction)
        {
            var nextStates = new List<ProgramState>(1);
            var next = currentState.Copy();
            next.IP += (ulong) instruction.Size;

            if (instruction.OpCode.Code == ILCode.VCALL)
            {
                // VCalls have embedded opcodes with different behaviours.
                nextStates.AddRange(_vCallProcessor.ProcessVCall(instruction, next));
            }
            else
            {
                // Push/pop necessary values from stack.
                int initial = next.Stack.Count;
                PopSymbolicValues(instruction, next);
                int popCount = initial - next.Stack.Count;

                initial = next.Stack.Count;
                PushSymbolicValues(instruction, next);
                int pushCount = next.Stack.Count - initial;

                // Apply control flow.
                PerformFlowControl(blockHeaders, instruction, nextStates, next);

                if (instruction.InferredMetadata == null)
                    instruction.InferredMetadata = new InferredMetadata();
                
                instruction.InferredMetadata.InferredPopCount = popCount;
                instruction.InferredMetadata.InferredPushCount = pushCount;
            }

            return nextStates;
        }

        private void PopSymbolicValues(ILInstruction instruction, ProgramState next)
        {
            var arguments = new List<SymbolicValue>(2);
            switch (instruction.OpCode.StackBehaviourPop)
            {
                case ILStackBehaviour.None:
                    break;
                
                case ILStackBehaviour.PopAny:
                case ILStackBehaviour.PopPtr:
                case ILStackBehaviour.PopByte:
                case ILStackBehaviour.PopWord:
                case ILStackBehaviour.PopDword:
                case ILStackBehaviour.PopQword:
                case ILStackBehaviour.PopReal32:
                case ILStackBehaviour.PopReal64:
                    var argument = next.Stack.Pop();
                    
                    if (instruction.OpCode.StackBehaviourPop != ILStackBehaviour.PopAny)
                        argument.Type = instruction.OpCode.StackBehaviourPop.GetArgumentType(0);
                    
                    // Check if instruction pops a value to a register.
                    if (instruction.OpCode.OperandType == ILOperandType.Register)
                        next.Registers[(VMRegisters) instruction.Operand] = new SymbolicValue(instruction);
                    
                    arguments.Add(argument);
                    break;
                
                case ILStackBehaviour.PopDword_PopDword:
                case ILStackBehaviour.PopQword_PopQword:
                case ILStackBehaviour.PopPtr_PopPtr:
                case ILStackBehaviour.PopPtr_PopObject:
                case ILStackBehaviour.PopPtr_PopByte:
                case ILStackBehaviour.PopPtr_PopWord:
                case ILStackBehaviour.PopPtr_PopDword:
                case ILStackBehaviour.PopPtr_PopQword:
                case ILStackBehaviour.PopObject_PopObject:
                case ILStackBehaviour.PopReal32_PopReal32:
                case ILStackBehaviour.PopReal64_PopReal64:
                    var argument2 = next.Stack.Pop();
                    var argument1 = next.Stack.Pop();

                    argument1.Type = instruction.OpCode.StackBehaviourPop.GetArgumentType(0);
                    argument2.Type = instruction.OpCode.StackBehaviourPop.GetArgumentType(1);
                    
                    arguments.Add(argument2);
                    arguments.Add(argument1);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Add instruction dependencies for data flow graph, in reverse order to negate natural stack behaviour.
            for (int i = arguments.Count - 1; i >= 0; i--)
                instruction.Dependencies.Add(arguments[i]);
        }

        private void PushSymbolicValues(ILInstruction instruction, ProgramState next)
        {
            switch (instruction.OpCode.StackBehaviourPush)
            {
                case ILStackBehaviour.None:
                    break;
                
                case ILStackBehaviour.PushPtr:
                case ILStackBehaviour.PushByte:
                case ILStackBehaviour.PushWord:
                case ILStackBehaviour.PushDword:
                case ILStackBehaviour.PushQword:
                case ILStackBehaviour.PushReal32:
                case ILStackBehaviour.PushReal64:
                case ILStackBehaviour.PushObject:
                case ILStackBehaviour.PushVar:
                    next.Stack.Push(new SymbolicValue(instruction)
                    {
                        Type = instruction.OpCode.StackBehaviourPush.GetResultType()
                    });
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void PerformFlowControl(ISet<long> blockHeaders, ILInstruction instruction, List<ProgramState> nextStates, ProgramState next)
        {
            switch (instruction.OpCode.FlowControl)
            {
                case ILFlowControl.Next:
                {
                    // Normal flow.
                    nextStates.Add(next);
                    break;
                }
                case ILFlowControl.Call:
                case ILFlowControl.Jump:
                {
                    blockHeaders.Add((long) next.IP);
                    
                    // Unconditional jump target.
                    var metadata = InferJumpTargets(instruction);
                    if (metadata != null)
                    {
                        next.IP = metadata.InferredJumpTargets[0];
                        blockHeaders.Add((long) next.IP);
                        nextStates.Add(next);
                    }

                    break;
                }
                case ILFlowControl.ConditionalJump:
                {
                    blockHeaders.Add((long) next.IP);
                    
                    // Next to normal jump target, we need to consider that either condition was false,
                    // or we returned from a call. Both have virtually the same effect on the flow analysis.
                    
                    var metadata = InferJumpTargets(instruction);

                    if (metadata != null)
                    {
                        var branch = next.Copy();
                        branch.IP = metadata.InferredJumpTargets[0]; // TODO: handle switch statements.
                        nextStates.Add(branch);
                        blockHeaders.Add((long) branch.IP);
                    }

                    nextStates.Add(next);
                    break;
                }
                case ILFlowControl.Return:
                {
                    blockHeaders.Add((long) next.IP);
                    // Return, do nothing.
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        private JumpMetadata InferJumpTargets(ILInstruction instruction)
        {
            try
            {
                var emulator = new InstructionEmulator();
                emulator.EmulateDependentInstructions(instruction);
            
                // After partial emulation, IP is on stack.
                var nextIp = emulator.Stack.Pop();
                Logger.Debug(Tag, $"Inferred edge IL_{instruction.Offset:X4} -> IL_{nextIp.U8:X4}");

                var metadata = new JumpMetadata(nextIp.U8);
                instruction.InferredMetadata = metadata;
                return metadata;
            }
            catch (NotSupportedException e)
            {
                Logger.Warning(Tag, "Could not infer jump target for " + instruction.Offset.ToString("X4") + ". " + e.Message);
            }

            return null;
        }

        private ControlFlowGraph BuildGraph(VMExportInfo export, IDictionary<long, ILInstruction> instructions, ICollection<long> blockHeaders)
        {
            var graph = new ControlFlowGraph();

            CollectBlocks(graph, instructions, blockHeaders);
            ConnectNodes(graph);

            graph.Entrypoint = graph.Nodes[graph.GetNodeName(export.CodeOffset)];
            return graph;
        }

        private static void CollectBlocks(ControlFlowGraph graph, IDictionary<long, ILInstruction> instructions, ICollection<long> blockHeaders)
        {
            Node currentNode = null;
            ILBasicBlock currentBlock = null;
            foreach (var instruction in instructions.OrderBy(x => x.Key).Select(x => x.Value))
            {
                // If current instruction is a basic block header, we start a new block. 
                if (currentNode == null || blockHeaders.Contains(instruction.Offset))
                {
                    currentNode = graph.Nodes.Add(graph.GetNodeName(instruction.Offset));
                    currentBlock = new ILBasicBlock();
                    currentNode.UserData[ILBasicBlock.BasicBlockProperty] = currentBlock;
                }

                // Add instruction to current block.
                currentBlock.Instructions.Add(instruction);

                // If next offset is also a header, we also create a new block.
                // This check is necessary as blocks might not appear in sequence after each other. 
                if (blockHeaders.Contains(instruction.Offset + instruction.Size))
                    currentNode = null;
            }
        }

        private static void ConnectNodes(ControlFlowGraph graph)
        {
            foreach (var node in graph.Nodes)
            {
                // Get the last instruction of the block.
                var block = (ILBasicBlock) node.UserData[ILBasicBlock.BasicBlockProperty];
                var last = block.Instructions[block.Instructions.Count - 1];
                long nextOffset = last.Offset + last.Size;

                // Add edges accordingly.
                switch (last.OpCode.FlowControl)
                {
                    case ILFlowControl.Next:
                        AddFallThroughEdge(graph, node, nextOffset);
                        break;
                    case ILFlowControl.Jump:
                        AddJumpTargetEdges(graph, node, last);
                        break;
                    case ILFlowControl.ConditionalJump:
                        AddJumpTargetEdges(graph, node, last);
                        AddFallThroughEdge(graph, node, nextOffset);
                        break;
                    case ILFlowControl.Call:
                        break;
                    case ILFlowControl.Return:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void AddFallThroughEdge(ControlFlowGraph graph, Node node, long nextOffset)
        {
            node.OutgoingEdges.Add(graph.GetNodeName(nextOffset));
        }

        private static void AddJumpTargetEdges(ControlFlowGraph graph, Node node, ILInstruction jump)
        {
            var jumpMetadata = (JumpMetadata) jump.InferredMetadata;
            foreach (var target in jumpMetadata.InferredJumpTargets)
                node.OutgoingEdges.Add(graph.GetNodeName((long) target));
        }
    }
}