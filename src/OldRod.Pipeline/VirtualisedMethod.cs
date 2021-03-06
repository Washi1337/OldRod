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

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Pipeline
{
    public class VirtualisedMethod
    {
        public VirtualisedMethod(VMFunction function)
        {
            Function = function;
        }
        
        public VirtualisedMethod(VMFunction function, uint exportId, VMExportInfo exportInfo)
        {
            Function = function;
            ExportId = exportId;
            ExportInfo = exportInfo;
        }

        public VMFunction Function
        {
            get;
        }

        public VMExportInfo ExportInfo
        {
            get;
        }
        public bool IsExport => ExportId != null;

        public uint? ExportId
        {
            get;
        }

        public MethodSignature MethodSignature
        {
            get;
            set;
        }

        public bool IsMethodSignatureInferred
        {
            get;
            set;
        }

        public MethodDefinition CallerMethod
        {
            get;
            set;
        }

        public ControlFlowGraph ControlFlowGraph
        {
            get;
            set;
        }

        public ILCompilationUnit ILCompilationUnit
        {
            get;
            set;
        }

        public CilCompilationUnit CilCompilationUnit
        {
            get;
            set;
        }

        public override string ToString()
        {
            return IsExport
                ? $"{Function} (Export {ExportId}, Method: {CallerMethod})"
                : $"{Function} (Method: {CallerMethod})";
        }
    }
}