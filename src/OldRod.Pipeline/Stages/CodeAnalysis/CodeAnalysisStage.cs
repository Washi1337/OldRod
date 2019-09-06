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
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using AsmResolver.Net.Signatures;
using OldRod.Core;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.Inference;
using OldRod.Core.Memory;
using OldRod.Core.Recompiler.Transform;

namespace OldRod.Pipeline.Stages.CodeAnalysis
{
    public class CodeAnalysisStage : IStage
    {
        public const string Tag = "CodeAnalysis";
        
        public string Name => "Code Analysis Stage";

        public IFrameLayoutDetector FrameLayoutDetector
        {
            get;
            set;
        } = new DefaultFrameLayoutDetector();
        
        public void Run(DevirtualisationContext context)
        {
            foreach (var method in context.VirtualisedMethods)
            {   
                // Detect stack frame layout of function.
                context.Logger.Debug(Tag, $"Detecting stack frame layout for function_{method.Function.EntrypointAddress:X4}...");
                method.Function.FrameLayout = method.IsExport
                    ? FrameLayoutDetector.DetectFrameLayout(context.Constants, context.TargetImage, method.ExportInfo)
                    : FrameLayoutDetector.DetectFrameLayout(context.Constants, context.TargetImage, method.Function);

                if (method.MethodSignature == null)
                {
                    // Create missing method signature based on the frame layout.
                    context.Logger.Debug(Tag, $"Inferring method signature from stack frame layout of function_{method.Function.EntrypointAddress:X4}...");
                    method.MethodSignature = CreateMethodSignature(context, method.Function.FrameLayout);
                    method.IsMethodSignatureInferred =
                        method.Function.References.All(r => r.ReferenceType != FunctionReferenceType.Ldftn);
                }
                
                if (method.CallerMethod == null)
                {
                    // Create methods for VM functions that are not mapped to any physical methods.
                    // This can happen if the VM method detection stage fails to map all methods to physical method defs,
                    // or the virtualised code refers to internal calls into the VM code.
                    context.Logger.Debug(Tag, $"Creating new physical method for function_{method.Function.EntrypointAddress:X4}...");
                    AddPhysicalMethod(context, method);
                }
            }
        }

        private static MethodSignature CreateMethodSignature(DevirtualisationContext context, IFrameLayout layout)
        {
            var methodSignature = new MethodSignature(layout.ReturnType ?? context.TargetImage.TypeSystem.Object);

            // Add parameters.
            for (int i = 0; i < layout.Parameters.Count; i++)
            {
                methodSignature.Parameters.Add(
                    new ParameterSignature(layout.Parameters[i].Type ?? context.TargetImage.TypeSystem.Object));
            }

            methodSignature.HasThis = layout.HasThis;
            
            return methodSignature;
        }

        private static void AddPhysicalMethod(DevirtualisationContext context, VirtualisedMethod method)
        {
            bool isHelperInit = method.ExportId == context.Constants.HelperInit;
            
            // Decide on a name of the new method.
            string name;
            if (!method.IsExport)
                name = "__VMFUNCTION__" + method.Function.EntrypointAddress.ToString("X4");
            else if (isHelperInit)
                name = "__VMHELPER_INIT__";
            else
                name = "__VMEXPORT__" + method.ExportId;

            // Create new physical method.
            var dummy = new MethodDefinition(name,
                MethodAttributes.Public,
                method.MethodSignature);
            dummy.IsStatic = !method.MethodSignature.HasThis;
            dummy.CilMethodBody = new CilMethodBody(dummy);
            method.CallerMethod = dummy;
            
            // We perform a heuristic analysis on all private members that are accessed in the code, as these are only
            // able to be accessed if the caller is in the same type.

            var inferredDeclaringType = TryInferDeclaringTypeFromMemberAccesses(context, method);

            // If that fails (e.g. there are no private member accesses), we check if the method is an instance member.
            // If it is, the this parameter is always the declaring type, EXCEPT for one scenario:
            // 
            // When the method is intra-linked and only referenced through a LDFTN function, the this parameter type
            // can be inaccurate. KoiVM does not care for the type of the this object, as everything is done through
            // reflection, so it reuses the method signature for things like instance event handlers (object, EventArgs),
            // even though the hidden this parameter might have had a different type.
            
            if (inferredDeclaringType == null && !dummy.IsStatic)
                inferredDeclaringType = TryInferDeclaringTypeFromThisParameter(context, dummy);

            if (inferredDeclaringType != null)
            {
                // We found a declaring type!
                context.Logger.Debug(Tag,
                    $"Inferred declaring type of function_{method.Function.EntrypointAddress:X4} ({inferredDeclaringType}).");
                
                // Remove this parameter from the method signature if necessary.
                if (!dummy.IsStatic)
                    dummy.Signature.Parameters.RemoveAt(0);
            }
            else
            {
                // Fallback method: Add to <Module> and make static.
                context.Logger.Debug(Tag, isHelperInit
                    ? $"Adding HELPER_INIT to <Module>."
                    : $"Could not infer declaring type of function_{method.Function.EntrypointAddress:X4}. Adding to <Module> instead.");

                dummy.IsStatic = true;
                method.MethodSignature.HasThis = false;
                inferredDeclaringType = context.TargetImage.Assembly.Modules[0].TopLevelTypes[0];
            }

            inferredDeclaringType.Methods.Add(dummy);
        }
        
        private static TypeDefinition TryInferDeclaringTypeFromMemberAccesses(
            DevirtualisationContext context,
            VirtualisedMethod method)
        {
            // Get all private member accesses.
            var privateMemberRefs = new HashSet<IMemberReference>();

            foreach (var instruction in method.Function.Instructions.Values)
            {
                IMemberProvider provider;
                
                var annotation = instruction.Annotation;
                switch (annotation)
                {
                    case IMemberProvider p:
                        provider = p;
                        break;

                    case CallAnnotation call:
                        var resolvedMethod = context.ResolveMethod(call.Function.EntrypointAddress);
                        if (resolvedMethod == null)
                            continue;
                        provider = new ECallAnnotation(resolvedMethod, VMECallOpCode.CALL);
                        break;
                    
                    default:
                        continue;
                }

                if (provider.Member.DeclaringType != null
                    && provider.Member.DeclaringType.ResolutionScope.GetAssembly() == context.TargetImage.Assembly
                    && provider.RequiresSpecialAccess)
                {
                    privateMemberRefs.Add(provider.Member);
                }
            }

            var types = new List<TypeDefinition>();
            foreach (var member in privateMemberRefs)
            {
                var memberDef = (IMemberDefinition) ((IResolvable) member).Resolve();
                var declaringTypes = GetDeclaringTypes(memberDef as TypeDefinition ?? memberDef.DeclaringType);
                types.Add(declaringTypes.First(t => memberDef.IsAccessibleFromType(t)));
            }

            if (types.Count == 0)
                return null;
            
            types.Sort((a, b) =>
            {
                if (a.IsAccessibleFromType(b))
                    return b.IsAccessibleFromType(a) ? 0 : 1;
                else
                    return b.IsAccessibleFromType(a) ? 0 : -1;
            });

            return types[0];
        }

        private static IList<TypeDefinition> GetDeclaringTypes(TypeDefinition type)
        {
            var types = new List<TypeDefinition>();
            while (type != null)
            {
                types.Add(type);
                type = type.DeclaringType;
            }

            types.Reverse();
            return types;
        }

        private static TypeDefinition GetCommonDeclaringType(IReadOnlyList<IList<TypeDefinition>> declaringTypes)
        {
            int shortestSequenceLength = declaringTypes.Min(x => x.Count);

            // Find the maximum index for which the hierarchies are still the same.
            for (int i = 0; i < shortestSequenceLength; i++)
            {
                // If any of the types at the current position is different, we have found the index.
                if (declaringTypes.Any(x => declaringTypes[0][i].FullName != x[i].FullName))
                    return i == 0 ? null : declaringTypes[0][i - 1];
            }

            // We've walked over all hierarchies, just pick the last one of the shortest hierarchy.
            return shortestSequenceLength > 0
                ? declaringTypes[0][shortestSequenceLength - 1]
                : null;
        }

        private static TypeDefinition TryInferDeclaringTypeFromThisParameter(DevirtualisationContext context,
            MethodDefinition dummy)
        {
            var thisType = dummy.Signature.Parameters[0].ParameterType;
            return thisType.ResolutionScope == context.TargetImage.Assembly.Modules[0]
                ? (TypeDefinition) thisType.ToTypeDefOrRef().Resolve()
                : null;
        }
    }
}