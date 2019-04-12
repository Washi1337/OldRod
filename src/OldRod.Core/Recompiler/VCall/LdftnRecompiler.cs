
using System;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.Annotations;

namespace OldRod.Core.Recompiler.VCall
{
    public class LdftnRecompiler : IVCallRecompiler 
    {
        public CilExpression Translate(RecompilerContext context, ILVCallExpression expression)
        {
            var annotation = (LdftnAnnotation) expression.Annotation;

            ICallableMemberReference method;
            if (!annotation.IsIntraLinked)
            {
                method = annotation.Method;
            }
            else
            {
                method = context.ExportResolver.ResolveExport(annotation.Function.EntrypointAddress);
                if (method == null)
                {
                    throw new RecompilerException(
                        $"Could not resolve function_{annotation.Function.EntrypointAddress:X4} to a physical method.");
                }
            }

            var result = new CilInstructionExpression(CilOpCodes.Ldftn, method)
            {
                ExpressionType = context.TargetImage.TypeSystem.IntPtr
            };
            
            return result;
        }
        
    }
}