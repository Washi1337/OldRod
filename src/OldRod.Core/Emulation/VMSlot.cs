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

using System.Runtime.InteropServices;

namespace OldRod.Core.Emulation
{
    [StructLayout(LayoutKind.Explicit)]
    public struct VMSlot
    {
        [FieldOffset(0)] 
        private ulong _u8;
        [FieldOffset(0)] 
        private double _r8;
        [FieldOffset(0)] 
        private uint _u4;
        [FieldOffset(0)] 
        private float _r4;
        [FieldOffset(0)] 
        private ushort _u2;
        [FieldOffset(0)] 
        private byte _u1;
        [FieldOffset(8)] 
        private object _o;

        public ulong U8
        {
            get => _u8;
            set
            {
                _u8 = value;
                _o = null;
            }
        }

        public uint U4
        {
            get => _u4;
            set
            {
                _u4 = value;
                _o = null;
            }
        }

        public ushort U2
        {
            get => _u2;
            set
            {
                _u2 = value;
                _o = null;
            }
        }

        public byte U1
        {
            get => _u1;
            set
            {
                _u1 = value;
                _o = null;
            }
        }

        public double R8
        {
            get => _r8;
            set
            {
                _r8 = value;
                _o = null;
            }
        }

        public float R4
        {
            get => _r4;
            set
            {
                _r4 = value;
                _o = null;
            }
        }

        public object O
        {
            get => _o;
            set
            {
                _o = value;
                _u8 = 0;
            }
        }
    }
}