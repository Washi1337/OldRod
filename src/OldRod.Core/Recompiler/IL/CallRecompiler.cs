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

using System.Linq;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Signatures;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Annotations;

namespace OldRod.Core.Recompiler.IL
{
    public class CallRecompiler : IOpCodeRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILInstructionExpression expression)
        {
            var callMetadata = (CallAnnotation) expression.Annotation;
            
            // Convert entrypoint address to physical method def.
            var method = context.ExportResolver.ResolveExport(callMetadata.Function.EntrypointAddress);
            var methodSig = ((MethodSignature) method.Signature);
            
            // Create call instruction.
            CilExpression result = new CilInstructionExpression(CilOpCodes.Call, method,
                context.RecompileCallArguments(method, expression.Arguments.Skip(1).ToArray()))
            {
                ExpressionType = methodSig.ReturnType
            };

            // Make sure the resulting object is converted to an unsigned integer if necessary.
            return result;

        }
    }
}