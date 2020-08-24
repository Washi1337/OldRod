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
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Recompiler.Transform;

namespace OldRod.Core.Recompiler
{
    public class RecompilerContext
    {
        private readonly Stack<GenericContext> _genericContexts = new Stack<GenericContext>();
        
        public RecompilerContext(CilMethodBody methodBody, ModuleDefinition targetModule,
            ILToCilRecompiler recompiler, IVMFunctionResolver exportResolver)
        {
            MethodBody = methodBody ?? throw new ArgumentNullException(nameof(methodBody));
            TargetModule = targetModule ?? throw new ArgumentNullException(nameof(targetModule));
            Recompiler = recompiler ?? throw new ArgumentNullException(nameof(recompiler));
            ExportResolver = exportResolver ?? throw new ArgumentNullException(nameof(exportResolver));
            ReferenceImporter = new ReferenceImporter(targetModule);
            TypeHelper = new TypeHelper(ReferenceImporter);
            _genericContexts.Push(new GenericContext(null, null));
        }

        public ILogger Logger
        {
            get;
            set;
        } = EmptyLogger.Instance;
        
        public CilMethodBody MethodBody
        {
            get;
        }

        public ModuleDefinition TargetModule
        {
            get;
        }

        public IVMFunctionResolver ExportResolver
        {
            get;
        }

        public ILToCilRecompiler Recompiler
        {
            get;
        }

        public ReferenceImporter ReferenceImporter
        {
            get;
        }

        public TypeHelper TypeHelper
        {
            get;
        }
        
        public IDictionary<ILVariable, CilVariable> Variables
        {
            get;
        } = new Dictionary<ILVariable, CilVariable>();

        public IDictionary<ILParameter, CilParameter> Parameters
        {
            get;
        } = new Dictionary<ILParameter, CilParameter>();
        
        public CilVariable FlagVariable
        {
            get;
            set;
        }

        public GenericContext GenericContext => _genericContexts.Peek();
        
        public void EnterMember(IMetadataMember member)
        {
            IGenericArgumentsProvider type = null;
            IGenericArgumentsProvider method = null;

            if (member is TypeSpecification typeSpec)
            {
                type = typeSpec.Signature as GenericInstanceTypeSignature;
            }
            else if (member is IMemberDescriptor memberRef)
            {
                if (memberRef.DeclaringType is TypeSpecification declaringType)
                    type = declaringType.Signature as GenericInstanceTypeSignature;
                if (member is MethodSpecification methodSpec)
                    method = methodSpec.Signature;
            }

            _genericContexts.Push(new GenericContext(type, method));
        }

        public void ExitMember()
        {
            _genericContexts.Pop();
        }
        
        public IList<CilExpression> RecompileCallArguments(
            IMethodDescriptor method, 
            IList<ILExpression> arguments,
            VMECallOpCode opCode,
            ITypeDescriptor constrainedType = null)
        {
            var methodSig = method.Signature;
            var result = new List<CilExpression>();
            
            // Emit arguments.
            for (var i = 0; i < arguments.Count; i++)
            {
                // Recompile argument.
                var cilArgument = (CilExpression) arguments[i].AcceptVisitor(Recompiler);

                // Figure out expected argument type.
                TypeSignature argumentType;
                if (methodSig.HasThis && opCode != VMECallOpCode.NEWOBJ)
                {
                    // Instance method invocation.
                    
                    if (i == 0)
                    {
                        // First parameter is the object instance that this method is called on (implicit this parameter).
                        argumentType = constrainedType?.ToTypeSignature() ?? method.DeclaringType.ToTypeSignature();
                        
                        // Calls on instance methods of value types need the this parameter to be passed on by-ref.
                        if (argumentType.IsValueType)
                            argumentType = new ByReferenceTypeSignature(argumentType);
                    }
                    else
                    {
                        argumentType = methodSig.ParameterTypes[i - 1];
                    }
                }
                else
                {
                    // Static method invocation.
                    argumentType = methodSig.ParameterTypes[i];
                }

                cilArgument.ExpectedType = argumentType.InstantiateGenericTypes(GenericContext);
                result.Add(cilArgument);
            }
            return result;
        }
        
    }
}