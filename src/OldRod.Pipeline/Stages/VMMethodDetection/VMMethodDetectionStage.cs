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

        private static readonly IList<string> Run1ExpectedTypes = new[]
        {
            "System.RuntimeTypeHandle",
            "System.UInt32",
            "System.Object[]"
        };
        
        private static readonly IList<string> Run2ExpectedTypes = new[]
        {
            "System.RuntimeTypeHandle",
            "System.UInt32",
            "System.Void*[]",
            "System.Void*",
        };

        public const string Tag = "VMMethodDetection";
        
        public string Name => "Virtualised method detection stage";

        public void Run(DevirtualisationContext context)
        {
            // KoiVM defines a type VMEntry to bootstrap the virtual machine for a particular method.
            // Therefore, to detect virtualised methods, we therefore have to detect this type first so that we can 
            // look for references to one of the Run methods.

            if (!context.Options.NoExportMapping)
                context.VMEntryInfo = ExtractVMEntryInfo(context);
            
            ConvertFunctionSignatures(context);
            
            if (context.Options.NoExportMapping)
            {
                context.Logger.Debug(Tag, "Not mapping methods to physical methods.");
            }
            else
            {
                context.Logger.Debug(Tag, "Mapping methods to physical methods...");
                MapVMExportsToMethods(context);
            }

            if (context.Options.RenameSymbols)
            {
                context.VMEntryInfo.VMEntryType.Namespace = "KoiVM.Runtime";
                context.VMEntryInfo.VMEntryType.Name = "VMEntry";
                context.VMEntryInfo.RunMethod1.Name = "Run";
                context.VMEntryInfo.RunMethod2.Name = "Run";
            }
        }

        private VMEntryInfo ExtractVMEntryInfo(DevirtualisationContext context)
        {
            if (context.Options.OverrideVMEntryToken)
            {
                // Use user-defined VMEntry type token instead of detecting.
                
                context.Logger.Debug(Tag, $"Using token {context.Options.VMEntryToken} for VMEntry type.");
                var type = (TypeDefinition) context.RuntimeModule.ResolveMember(context.Options.VMEntryToken.Value);
                var info = TryExtractVMEntryInfoFromType(context, type);
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
            foreach (var type in context.RuntimeModule.Assembly.Modules[0].TopLevelTypes)
            {
                // TODO: maybe a better matching criteria is required here.
                if (type.Methods.Count >= 5) 
                {
                    var info = TryExtractVMEntryInfoFromType(context, type);
                    if (info != null)
                        return info;
                }
            }

            return null;
        }

        private static bool HasParameterTypes(MethodDefinition method, ICollection<string> expectedTypes)
        {
            expectedTypes = new List<string>(expectedTypes);

            foreach (var parameter in method.Signature.Parameters)
            {
                string typeFullName = parameter.ParameterType.FullName;
                
                if (!expectedTypes.Contains(typeFullName))
                    return false;
                
                expectedTypes.Remove(typeFullName);
            }

            return true;
        }

        private VMEntryInfo TryExtractVMEntryInfoFromType(DevirtualisationContext context, TypeDefinition type)
        {
            var info = new VMEntryInfo
            {
                VMEntryType = type
            };

            foreach (var method in type.Methods)
            {
                switch (method.Signature.Parameters.Count)
                {
                    case 3:
                        if (HasParameterTypes(method, Run1ExpectedTypes))
                            info.RunMethod1 = method;
                        break;
                    case 4:
                        if (HasParameterTypes(method, Run2ExpectedTypes))
                            info.RunMethod2 = method;
                        break;
                }
            }

            if (info.RunMethod1 == null || info.RunMethod2 == null)
                return null;
            
            return info;
        }

        private void ConvertFunctionSignatures(DevirtualisationContext context)
        {
            foreach (var entry in context.KoiStream.Exports.Where(x => !x.Value.IsSignatureOnly))
            {
                context.Logger.Debug(Tag, $"Converting VM signature of export {entry.Key} to method signature...");
                context.VirtualisedMethods.Add(
                    new VirtualisedMethod(new VMFunction(entry.Value.EntrypointAddress, entry.Value.EntryKey), entry.Key,
                        entry.Value)
                    {
                        MethodSignature = VMSignatureToMethodSignature(context, entry.Value.Signature)
                    });
            }
        }

        private void MapVMExportsToMethods(DevirtualisationContext context)
        {
            int matchedMethods = 0;
            
            // Go over all methods in the assembly and detect whether it is virtualised by looking for a call 
            // to the VMEntry.Run method. If it is, also detect the export ID associated to it to define a mapping
            // between VMExport and physical method. 
            foreach (var type in context.TargetModule.Assembly.Modules[0].GetAllTypes())
            {
                foreach (var method in type.Methods)
                {
                    if (!context.Options.SelectedMethods.Contains(method.MetadataToken.Rid))
                        continue;
                    
                    var matchingVmMethods = GetMatchingVirtualisedMethods(context, method);

                    if (matchingVmMethods.Count > 0
                        && method.CilMethodBody != null
                        && TryExtractExportTypeFromMethodBody(context, method.CilMethodBody, out int exportId))
                    {
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
                
            // There could be more exports defined in the #Koi md stream than we were able to directly match
            // with methods in the target assembly. It is expected that the HELPER_INIT method is not matched to a
            // physical method definition, but it could also be that we missed one due to some other form of
            // obfuscation applied to it (maybe a fork of the vanilla version). 
            
            // These missing physical methods will later be added, together with the internal functions.
            
            // Warn if there are more than one method not directly mapped to a physical method definition.
            if (matchedMethods < context.VirtualisedMethods.Count - 1)
            {
                context.Logger.Warning(Tag, $"Not all VM exports were mapped to physical method definitions "
                                            + $"({matchedMethods} out of {context.VirtualisedMethods.Count} were mapped). "
                                            + "Dummies will be added to the assembly for the remaining exports.");
            }
        }

        private bool TryExtractExportTypeFromMethodBody(DevirtualisationContext context, CilMethodBody methodBody, out int exportId)
        {
            exportId = 0;

            var instructions = methodBody.Instructions;
            var runCall = instructions.FirstOrDefault(x =>
                x.OpCode.Code == CilCode.Call
                && x.Operand is IMethodDefOrRef methodOperand
                && (Comparer.Equals(context.VMEntryInfo.RunMethod1, methodOperand)
                    || Comparer.Equals(context.VMEntryInfo.RunMethod2, methodOperand)
                ));
            
            if (runCall != null)
            {   
                // Do a very minimal emulation of the method body, we are only interested in ldc.i4 values that push
                // the export ID. All other values on the stack will have a placeholder of -1.
                
                // Note that this strategy only works for variants of KoiVM that have exactly one constant integer
                // pushed on the stack upon calling the run method. It does NOT detect the export ID when the constant
                // is masked behind some obfuscation, or when there are multiple integer parameters pushed on the stack. 
                
                var stack = new Stack<int>();
                foreach (var instr in instructions)
                {
                    if (instr.Offset == runCall.Offset)
                    {
                        // We reached the call to the run method, obtain the integer value corresponding to the export id.
                        
                        int argCount = instr.GetStackPopCount(methodBody);
                        for (int i = 0; i < argCount; i++)
                        {
                            int value = stack.Pop();
                            if (value != -1)
                            {
                                exportId = value;
                                return true;
                            }
                        }
                        
                        return false;
                    }

                    if (instr.IsLdcI4)
                    {
                        // Push the ldc.i4 value if we reach one.
                        stack.Push(instr.GetLdcValue());
                    }
                    else
                    {
                        // Pop the correct amount of values from the stack, and push placeholders.
                        for (int i = 0; i < instr.GetStackPopCount(methodBody); i++)
                            stack.Pop();
                        for (int i = 0; i < instr.GetStackPushCount(methodBody); i++)
                            stack.Push(-1);
                    }
                }
            }

            return false;
        }

        private ICollection<VirtualisedMethod> GetMatchingVirtualisedMethods(
            DevirtualisationContext context,
            MethodDefinition methodToMatch)
        {
            var matches = new List<VirtualisedMethod>();
            
            foreach (var vmMethod in context.VirtualisedMethods.Where(x => x.CallerMethod == null))
            {
                if (Comparer.Equals(methodToMatch.Signature, vmMethod.MethodSignature))
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
            var resolvedType = ((ITypeDescriptor) context.TargetModule.ResolveMember(token));
            return context.TargetModule.TypeSystem.GetMscorlibType(resolvedType)
                   ?? context.ReferenceImporter.ImportTypeSignature(resolvedType.ToTypeSignature());
        }
    }
}