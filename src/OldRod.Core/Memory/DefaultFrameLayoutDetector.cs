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

using AsmResolver.Net;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Core.Memory
{
    public class DefaultFrameLayoutDetector : IFrameLayoutDetector
    {
        public IFrameLayout DetectFrameLayout(VMConstants constants, VMFunction function)
        {
            // TODO:
            return new DefaultFrameLayout(0, 0, true);
        }

        public IFrameLayout DetectFrameLayout(VMConstants constants, MetadataImage image, VMExportInfo export)
        {
            var returnType = (ITypeDefOrRef) image.ResolveMember(export.Signature.ReturnToken);
            return new DefaultFrameLayout(
                export.Signature.ParameterTokens.Count, 
                0, 
                !returnType.IsTypeOf("System", "Void"));
        }
    }
}