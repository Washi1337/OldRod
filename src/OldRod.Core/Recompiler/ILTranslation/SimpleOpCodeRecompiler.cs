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
            : this(newOpCode, opCodes.AsEnumerable())
        {
        }
        
        public SimpleOpCodeRecompiler(CilOpCode newOpCode, IEnumerable<ILCode> opCodes)
        {
            NewOpCode = newOpCode;
            OpCodes = new HashSet<ILCode>(opCodes);
        }
        
        public ISet<ILCode> OpCodes
        {
            get;
        }

        public CilOpCode NewOpCode
        {
            get;
        }

        public VMFlags AffectedFlags
        {
            get;
            set;
        }

        public bool AffectsFlags
        {
            get;
            set;
        }
        
        public virtual CilExpression Translate(RecompilerContext context, ILInstructionExpression expression)
        {
            if (!OpCodes.Contains(expression.OpCode.Code))
                throw new NotSupportedException();
            
            var result = new CilInstructionExpression(NewOpCode);

            // Add arguments.
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

            result.ShouldEmitFlagsUpdate = AffectsFlags;
            if (AffectsFlags) 
                result.AffectedFlags = AffectedFlags;

            return result;
        }
    }
}