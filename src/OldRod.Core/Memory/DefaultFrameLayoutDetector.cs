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

using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Memory
{
    public class DefaultFrameLayoutDetector : IFrameLayoutDetector
    {
        private readonly VMConstants _constants;

        public DefaultFrameLayoutDetector(VMConstants constants)
        {
            _constants = constants;
        }

        public IFrameLayout DetectFrameLayout(VMFunction function)
        {
            // TODO:
            return new DefaultFrameLayout(0, 0);
        }

        public IFrameLayout DetectFrameLayout(VMExportInfo export)
        {
            int parameterCount = export.Signature.ParameterTokens.Count;
            if ((export.Signature.Flags & _constants.FlagInstance) != 0)
                parameterCount++;
            
            return new DefaultFrameLayout(export.Signature.ParameterTokens.Count, 0);
        }
    }
}