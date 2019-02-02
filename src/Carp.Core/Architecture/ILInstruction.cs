using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cil;
using Carp.Core.Disassembly;
using Carp.Core.Disassembly.DataFlow;

namespace Carp.Core.Architecture
{
    public class ILInstruction
    {
        public ILInstruction(int offset, ILOpCode opCode, object operand)
        {
            Offset = offset;
            OpCode = opCode;
            Operand = operand;
        }
        
        public int Offset
        {
            get;
            set;
        }
        
        public ILOpCode OpCode
        {
            get;
            set;
        }

        public object Operand
        {
            get;
            set;
        }

        public ProgramState ProgramState
        {
            get;
            set;
        }

        public IList<SymbolicValue> Dependencies
        {
            get;
            set;
        } = new List<SymbolicValue>();

        public int Size
        {
            get
            {
                switch (OpCode.OperandType)
                {
                    case ILOperandType.None:
                        return 2;
                    case ILOperandType.Register:
                        return 3;
                    case ILOperandType.ImmediateDword:
                        return 6;
                    case ILOperandType.ImmediateQword:
                        return 10;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override string ToString()
        {
            return $"IL_{Offset:X4}: {OpCode} {Operand}";
        }

        public IEnumerable<ILInstruction> GetAllDependencies()
        {
            foreach (var source in Dependencies
                .SelectMany(x => x.DataSources))
            {
                yield return source;
                foreach (var dep in source.GetAllDependencies())
                    yield return dep;
            }
        }

    }
}