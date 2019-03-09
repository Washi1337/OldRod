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

using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler
{
    public class RecompilerContext
    {
        public RecompilerContext(CilMethodBody methodBody, MetadataImage targetImage,
            ILToCilRecompiler recompiler)
        {
            MethodBody = methodBody;
            TargetImage = targetImage;
            Recompiler = recompiler;
            ReferenceImporter = new ReferenceImporter(targetImage);
        }

        public CilMethodBody MethodBody
        {
            get;
        }

        public MetadataImage TargetImage
        {
            get;
        }

        public ILToCilRecompiler Recompiler
        {
            get;
        }

        public ReferenceImporter ReferenceImporter
        {
            get;
        }
        
        public IDictionary<ILVariable, VariableSignature> Variables
        {
            get;
        } = new Dictionary<ILVariable, VariableSignature>();

        public IDictionary<ILParameter, ParameterSignature> Parameters
        {
            get;
        } = new Dictionary<ILParameter, ParameterSignature>();
        
        public VariableSignature FlagVariable
        {
            get;
            set;
        }
    }
}