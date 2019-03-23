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
using AsmResolver.Net;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Metadata;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly.Inference;

namespace OldRod.Pipeline.Stages.VMMethodDetection
{
    public class VMMethodDetectionStage : IStage
    {
        private static readonly SignatureComparer Comparer = new SignatureComparer
        {
            IgnoreAssemblyVersionNumbers = true
        };
        
        public const string Tag = "VMMethodDetection";
        
        public string Name => "Virtualised method detection stage";

        public void Run(DevirtualisationContext context)
        {
            // KoiVM defines a type VMEntry to bootstrap the virtual machine for a particular method.
            // Therefore, to detect virtualised methods, we therefore have to detect this type first so that we can 
            // look for references to one of the Run methods.

            context.VMEntryInfo = ExtractVMEntryInfo(context);
            MapVMExportsToMethods(context);
        }

        private VMEntryInfo ExtractVMEntryInfo(DevirtualisationContext context)
        {
            if (context.Options.OverrideVMEntryToken)
            {
                // Use user-defined VMEntry type token instead of detecting.
                
                context.Logger.Debug(Tag, "Using token " + context.Options.VMEntryToken + " for VMEntry type.");
                var type = (TypeDefinition) context.RuntimeImage.ResolveMember(context.Options.VMEntryToken);
                var info = TryExtractVMEntryInfoFromType(type);
                if (info == null)
                {
                    throw new DevirtualisationException(
                        $"Type {type.MetadataToken} ({type}) does not match the signature of the VMEntry type.");
                }

                return info;
            }
            else
            {
                // Attempt to auto-detect the VMEntry type.
                
                context.Logger.Debug(Tag, "Searching for VMEntry type...");
                var info = SearchVMEntryType(context);
                
                if (info == null)
                    throw new DevirtualisationException("Could not detect VMEntry type.");
                
                context.Logger.Debug(Tag, $"Detected VMEntry type ({info.VMEntryType.MetadataToken})");
                return info;
            }
        }

        private VMEntryInfo SearchVMEntryType(DevirtualisationContext context)
        {
            foreach (var type in context.RuntimeImage.Assembly.Modules[0].TopLevelTypes)
            {
                // TODO: maybe a better matching criteria is required here.
                if (type.Methods.Count >= 5) 
                {
                    var info = TryExtractVMEntryInfoFromType(type);
                    if (info != null)
                        return info;
                }
            }

            return null;
        }

        private VMEntryInfo TryExtractVMEntryInfoFromType(TypeDefinition type)
        {
            var info = new VMEntryInfo
            {
                VMEntryType = type
            };

            foreach (var method in type.Methods)
            {
                var parameters = method.Signature.Parameters;
                switch (parameters.Count)
                {
                    case 3:
                    {
                        if (parameters[0].ParameterType.IsTypeOf("System", "RuntimeTypeHandle")
                            && parameters[1].ParameterType.IsTypeOf("System", "UInt32")
                            && parameters[2].ParameterType is SzArrayTypeSignature arrayType
                            && arrayType.BaseType.IsTypeOf("System", "Object"))
                        {
                            info.RunMethod1 = method;
                        }

                        break;
                    }
                    case 4:
                    {
                        if (parameters[0].ParameterType.IsTypeOf("System", "RuntimeTypeHandle")
                            && parameters[1].ParameterType.IsTypeOf("System", "UInt32")
                            && parameters[2].ParameterType is SzArrayTypeSignature arrayType
                            && arrayType.BaseType is PointerTypeSignature pointerType1
                            && pointerType1.BaseType.IsTypeOf("System", "Void")
                            && parameters[3].ParameterType is PointerTypeSignature pointerType2
                            && pointerType2.BaseType.IsTypeOf("System", "Void"))
                        {
                            info.RunMethod2 = method;
                        }

                        break;
                    }
                }
            }

            if (info.RunMethod1 == null || info.RunMethod2 == null)
                return null;
            return info;
        }

        private void ConvertFunctionSignatures(DevirtualisationContext context)
        {
            foreach (var entry in context.KoiStream.Exports)
            {
                context.Logger.Debug(Tag, $"Converting VM signature of export {entry.Key} to method signature...");
                context.VirtualisedMethods.Add(
                    new VirtualisedMethod(new VMFunction(entry.Value.CodeOffset, entry.Value.EntryKey), entry.Key,
                        entry.Value)
                    {
                        ConvertedMethodSignature = VMSignatureToMethodSignature(context, entry.Value.Signature)
                    });
            }
        }

        private void MapVMExportsToMethods(DevirtualisationContext context)
        {
            var moduleType = context.TargetImage.Assembly.Modules[0].TopLevelTypes[0];

            // Convert VM function signatures to .NET method signatures for easier mapping of methods.
            ConvertFunctionSignatures(context);

            int matchedMethods = 0;

            // Go over all methods in the assembly and detect whether it is virtualised by looking for a call 
            // to the VMEntry.Run method. If it is, also detect the export ID associated to it to define a mapping
            // between VMExport and physical method. 
            foreach (var type in context.TargetImage.Assembly.Modules[0].GetAllTypes())
            {
                foreach (var method in type.Methods)
                {
                    var matchingVmMethods = GetMatchingVirtualisedMethods(context, method);

                    if (matchingVmMethods.Count > 0 && method.CilMethodBody != null)
                    {
                        // TODO: more thorough pattern matching is possibly required.
                        //       make more generic, maybe partial emulation here as well?

                        var instructions = method.CilMethodBody.Instructions;
                        if (instructions.Any(x => x.OpCode.Code == CilCode.Call
                                                  && Comparer.Equals(context.VMEntryInfo.RunMethod1,
                                                      (IMethodDefOrRef) x.Operand)))
                        {
                            int exportId = instructions[1].GetLdcValue();
                            context.Logger.Debug(Tag, $"Detected call to export {exportId} in {method}.");
                            var vmMethod = matchingVmMethods.FirstOrDefault(x => x.ExportId == exportId);
                            if (vmMethod != null)
                                vmMethod.CallerMethod = method;
                            else
                                context.Logger.Debug(Tag, $"Ignoring call to export {exportId} in {method}.");
                            matchedMethods++;
                        }
                    }
                }
            }

            // There could be more exports defined in the #Koi md stream than we were able to directly match
            // with methods in the target assembly. It is expected that the HELPER_INIT method is not matched to a
            // physical method definition, but it could also be that we missed one due to some other form of
            // obfuscation applied to it (maybe a fork of the vanilla version).
            
            // Warn if there are more than one method not directly mapped to a physical method definition.
            if (matchedMethods < context.VirtualisedMethods.Count - 1)
            {
                context.Logger.Warning(Tag, $"Not all VM exports were mapped to physical method definitions "
                                            + $"({matchedMethods} out of {context.VirtualisedMethods.Count} were mapped). "
                                            + "Dummies will be added to the assembly for the remaining exports.");
            }

            // Create dummy methods for the ones that are not mapped to any physical method.
            foreach (var vmMethod in context.VirtualisedMethods.Where(x => x.CallerMethod == null))
            {
                bool isHelperInit = vmMethod.ExportId == context.Constants.HelperInit;

                var dummy = new MethodDefinition(isHelperInit
                        ? "__VMHELPER_INIT__"
                        : "__VMEXPORT__" + vmMethod.ExportId,
                    MethodAttributes.Public | MethodAttributes.Static,
                    vmMethod.ConvertedMethodSignature);

                dummy.CilMethodBody = new CilMethodBody(dummy);
                vmMethod.CallerMethod = dummy;
                moduleType.Methods.Add(dummy);
            }

        }

        private ICollection<VirtualisedMethod> GetMatchingVirtualisedMethods(
            DevirtualisationContext context,
            MethodDefinition methodToMatch)
        {
            var matches = new List<VirtualisedMethod>();
            
            foreach (var vmMethod in context.VirtualisedMethods.Where(x => x.CallerMethod == null))
            {
                if (Comparer.Equals(methodToMatch.Signature, vmMethod.ConvertedMethodSignature))
                    matches.Add(vmMethod);
            }

            return matches;
        }

        private MethodSignature VMSignatureToMethodSignature(DevirtualisationContext context, VMFunctionSignature signature)
        {
            var returnType = GetTypeSig(context, signature.ReturnToken);
            var parameterTypes = signature.ParameterTokens.Select(x => GetTypeSig(context, x));

            bool hasThis = (signature.Flags & context.Constants.FlagInstance) != 0;

            return new MethodSignature(parameterTypes.Skip(hasThis ? 1 : 0), returnType)
            {
                HasThis = hasThis
            };
        }

        private TypeSignature GetTypeSig(DevirtualisationContext context, MetadataToken token)
        {
            var resolvedType = ((ITypeDescriptor) context.TargetImage.ResolveMember(token));
            return context.TargetImage.TypeSystem.GetMscorlibType(resolvedType)
                   ?? context.ReferenceImporter.ImportTypeSignature(resolvedType.ToTypeSignature());
        }
    }
}