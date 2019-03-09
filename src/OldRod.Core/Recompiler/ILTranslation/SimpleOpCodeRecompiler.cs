using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;

namespace OldRod.Core.Recompiler.ILTranslation
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