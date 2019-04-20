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

namespace OldRod.Core.Recompiler.VCall
{
    public class InitObjRecompiler : IVCallRecompiler
    {
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            var annotation = (TypeAnnotation) expression.Annotation;

            var argument = (CilExpression) expression.Arguments[expression.Arguments.Count - 1]
                .AcceptVisitor(context.Recompiler);

            argument.ExpectedType = new PointerTypeSignature(
                context.ReferenceImporter.ImportTypeSignature(annotation.Type.ToTypeSignature()));
            
            var result = new CilInstructionExpression(CilOpCodes.Initobj, annotation.Type, argument)
            {
                ExpressionType = null
            };

            return result;
        }
        
    }
}