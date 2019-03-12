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
using AsmResolver;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly
{
    public class InstructionDecoder
    {
        private readonly VMConstants _constants;

        public InstructionDecoder(VMConstants constants, IBinaryStreamReader reader, uint key)
        {
            _constants = constants;
            Reader = reader;
            CurrentKey = key;
        }
        
        public IBinaryStreamReader Reader
        {
            get;
        }

        public uint CurrentKey
        {
            get;
            set;
        }

        public ILInstruction ReadNextInstruction()
        {
            int offset = (int) Reader.Position;
            var opcode = ReadNextOpCode();
            var operand = ReadNextOperand(opcode.OperandType);
            return new ILInstruction(offset, opcode, operand);
        }

        private byte ReadByte()
        {
            uint key = CurrentKey;
            byte rawValue = Reader.ReadByte();
            byte b = (byte) (rawValue ^ key);
            key = key * 7 + b;
            CurrentKey = key;
            return b;
        }

        private ILOpCode ReadNextOpCode()
        {
            long offset = Reader.Position;
            
            var b = ReadByte();
            ReadByte();
            
            if (!_constants.OpCodes.TryGetValue(b, out var mappedOpCode))
                throw new DisassemblyException($"Byte {b:X2} at offset {offset:X4} not recognized as a valid opcode.");

            return ILOpCodes.All[(int) mappedOpCode];
        }

        private VMRegisters ReadRegister()
        {
            return _constants.Registers[ReadByte()];
        }

        private uint ReadDword()
        {
            return ReadByte()
                   | ((uint) ReadByte() << 8)
                   | ((uint) ReadByte() << 16)
                   | ((uint) ReadByte() << 24);
        }

        private ulong ReadQword()
        {
            return ReadByte()
                   | ((ulong) ReadByte() << 8)
                   | ((ulong) ReadByte() << 16)
                   | ((ulong) ReadByte() << 24)
                   | ((ulong) ReadByte() << 32)
                   | ((ulong) ReadByte() << 40)
                   | ((ulong) ReadByte() << 48)
                   | ((ulong) ReadByte() << 56);
        }

        private object ReadNextOperand(ILOperandType operandType)
        {
            switch (operandType)
            {
                case ILOperandType.None:
                    return null;
                case ILOperandType.Register:
                    return ReadRegister();
                case ILOperandType.ImmediateDword:
                    return ReadDword();
                case ILOperandType.ImmediateQword:
                    return ReadQword();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}