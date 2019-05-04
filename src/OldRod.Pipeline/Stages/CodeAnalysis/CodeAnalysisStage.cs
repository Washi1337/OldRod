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
using AsmResolver.Net.Metadata;
using AsmResolver.Net.Signatures;
using OldRod.Core.Disassembly.Annotations;
using OldRod.Core.Disassembly.Inference;
using OldRod.Core.Memory;

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

            return methodSignature;
        }

        private static void AddPhysicalMethod(DevirtualisationContext context, VirtualisedMethod method)
        {
            // Decide on a name of the new method.
            string name;
            if (!method.IsExport)
                name = "__VMFUNCTION__" + method.Function.EntrypointAddress.ToString("X4");
            else if (method.ExportId == context.Constants.HelperInit)
                name = "__VMHELPER_INIT__";
            else
                name = "__VMEXPORT__" + method.ExportId;
            
            // Create new method.
            var dummy = new MethodDefinition(name,
                MethodAttributes.Public,
                method.MethodSignature);
            dummy.IsStatic = !method.MethodSignature.HasThis;
            dummy.CilMethodBody = new CilMethodBody(dummy);
            method.CallerMethod = dummy;

            // Try to infer the declaring type of the method from references to private members.
            
            // Get all private member accesses.
            var privateMemberRefs = method.Function.Instructions.Values
                    .Select(i => i.Annotation)
                    .OfType<IMemberProvider>()
                    .Where(a => a.Member.DeclaringType != null
                                && a.Member.DeclaringType.ResolutionScope == context.TargetImage.Assembly.Modules[0]
                                && a.RequiresSpecialAccess)
                    .Select(a => a.Member)
#if DEBUG
                    .ToArray()
#endif
                ;

            // Get all declaring type chains.
            var declaringTypes = privateMemberRefs
                .Select(m => GetDeclaringTypes((TypeDefinition) m.DeclaringType.Resolve()))
                .ToArray();

            if (declaringTypes.Length > 0)
            {
                // Add to common declaring type.
                var commonDeclaringType = GetCommonDeclaringType(declaringTypes);
                context.Logger.Debug(Tag,
                    $"Inferred declaring type of function_{method.Function.EntrypointAddress:X4} ({commonDeclaringType}).");
                commonDeclaringType.Methods.Add(dummy);
            }
            
            if (dummy.DeclaringType == null)
            {
                // Fallback method: Add to <Module> and make static.
                context.Logger.Debug(Tag,
                    $"Could not infer declaring type of function_{method.Function.EntrypointAddress:X4}. Adding to <Module> instead.");
                var moduleType = context.TargetImage.Assembly.Modules[0].TopLevelTypes[0];
                dummy.IsStatic = true;
                method.MethodSignature.HasThis = false;
                moduleType.Methods.Add(dummy);
            }
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
    }
}