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
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.IL
{
    public class SimpleOpCodeRecompiler : IOpCodeRecompiler
    {
        public SimpleOpCodeRecompiler(CilOpCode newOpCode, params ILCode[] opCodes)
            : this(new[] {CilInstruction.Create(newOpCode)}, opCodes.AsEnumerable())
        {
        }

        public SimpleOpCodeRecompiler(IEnumerable<CilOpCode> newOpCodes, params ILCode[] opCodes)
            : this(newOpCodes.Select(x => CilInstruction.Create(x)), opCodes.AsEnumerable())
        {
        }

        public SimpleOpCodeRecompiler(IEnumerable<CilInstruction> newInstructions, IEnumerable<ILCode> opCodes)
        {
            NewInstructions = new List<CilInstruction>(newInstructions);
            SupportedOpCodes = new HashSet<ILCode>(opCodes);
        }

        public IList<CilInstruction> NewInstructions
        {
            get;
        }

        public ISet<ILCode> SupportedOpCodes
        {
            get;
        }

        public bool InvertedFlagsUpdate
        {
            get;
            set;
        }

        public virtual CilExpression Translate(RecompilerContext context, ILInstructionExpression expression)
        {
            if (!SupportedOpCodes.Contains(expression.OpCode.Code))
                throw new NotSupportedException();

            var result = new CilInstructionExpression();
            
            // Copy instructions
            foreach (var instruction in NewInstructions)
                result.Instructions.Add(new CilInstruction(0, instruction.OpCode, instruction.Operand));

            // Create arguments
            for (var i = 0; i < expression.Arguments.Count; i++)
            {
                // Convert argument.
                var argument = expression.Arguments[i];
                var cilArgument = (CilExpression) argument.AcceptVisitor(context.Recompiler);
                
                // Check type.
                var returnType = expression.OpCode.StackBehaviourPop
                    .GetArgumentType(i)
                    .ToMetadataType(context.TargetImage)
                    .ToTypeDefOrRef();
                
                // Convert if necessary, and add to argument list.
                result.Arguments.Add(cilArgument.EnsureIsType(context.ReferenceImporter.ImportType(returnType)));
            }
         
            // Determine expression type from opcode.
            result.ExpressionType = expression.OpCode.StackBehaviourPush
                .GetResultType()
                .ToMetadataType(context.TargetImage);

            result.ShouldEmitFlagsUpdate = expression.IsFlagDataSource;
            if (expression.IsFlagDataSource)
            {
                result.AffectedFlags = expression.OpCode.AffectedFlags;
                result.InvertedFlagsUpdate = InvertedFlagsUpdate;
            }

            return result;
        }
    }
}