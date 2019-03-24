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
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using AsmResolver.Net.Signatures;
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
                    : FrameLayoutDetector.DetectFrameLayout(context.Constants, method.Function);

                if (method.ConvertedMethodSignature == null)
                {
                    // Create missing method signature based on the frame layout.
                    
                    context.Logger.Debug(Tag, $"Inferring method signature from stack frame layout of function{method.Function.EntrypointAddress:X4}...");
                    method.ConvertedMethodSignature = CreateMethodSignature(context, method.Function.FrameLayout);
                }
                
                if (method.CallerMethod == null)
                {
                    // Create methods for VM functions that are not mapped to any physical methods.
                    // This can happen if the VM method detection stage fails to map all methods to physical method defs,
                    // or the virtualised code refers to internal calls into the VM code.
                    
                    context.Logger.Debug(Tag, $"Creating new physical method for function{method.Function.EntrypointAddress:X4}...");
                    AddPhysicalMethod(context, method);
                }
            }
        }

        private static MethodSignature CreateMethodSignature(DevirtualisationContext context, IFrameLayout layout)
        {
            var methodSignature = new MethodSignature(layout.ReturnsValue
                ? context.TargetImage.TypeSystem.Object
                : context.TargetImage.TypeSystem.Void);

            // Add parameters.
            for (int i = 0; i < layout.Parameters.Count; i++)
                methodSignature.Parameters.Add(new ParameterSignature(context.TargetImage.TypeSystem.Object));
            
            return methodSignature;
        }

        private static void AddPhysicalMethod(DevirtualisationContext context, VirtualisedMethod method)
        {
            var moduleType = context.TargetImage.Assembly.Modules[0].TopLevelTypes[0];

            string name;
            if (!method.IsExport)
                name = "__VMFUNCTION__" + method.Function.EntrypointAddress.ToString("X4");
            else if (method.ExportId == context.Constants.HelperInit)
                name = "__VMHELPER_INIT__";
            else
                name = "__VMEXPORT__" + method.ExportId;

            var dummy = new MethodDefinition(name,
                MethodAttributes.Public | MethodAttributes.Static,
                method.ConvertedMethodSignature);

            dummy.CilMethodBody = new CilMethodBody(dummy);
            method.CallerMethod = dummy;
            moduleType.Methods.Add(dummy);
        }
        
    }
}