
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
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

            IMethodDescriptor method;
            if (!annotation.IsIntraLinked)
            {
                method = annotation.Method;
            }
            else
            {
                method = context.ExportResolver.ResolveMethod(annotation.Function.EntrypointAddress);
                if (method == null)
                {
                    throw new RecompilerException(
                        $"Could not resolve function_{annotation.Function.EntrypointAddress:X4} to a physical method.");
                }
            }

            var result = new CilInstructionExpression(CilOpCodes.Ldftn, method)
            {
                ExpressionType = context.TargetModule.CorLibTypeFactory.IntPtr
            };

            if (annotation.IsVirtual)
            {
                var callingObject = (CilExpression) expression.Arguments[expression.Arguments.Count - 1]
                    .AcceptVisitor(context.Recompiler);
                result.Instructions[0].OpCode = CilOpCodes.Ldvirtftn;
                result.Arguments.Add(callingObject);
            }
            
            return result;
        }
        
    }
}