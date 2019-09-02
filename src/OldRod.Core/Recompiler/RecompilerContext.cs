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
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Recompiler.Transform;

namespace OldRod.Core.Recompiler
{
    public class RecompilerContext
    {
        private readonly Stack<IGenericContext> _genericContexts = new Stack<IGenericContext>();
        
        public RecompilerContext(CilMethodBody methodBody, MetadataImage targetImage,
            ILToCilRecompiler recompiler, IVMFunctionResolver exportResolver)
        {
            MethodBody = methodBody ?? throw new ArgumentNullException(nameof(methodBody));
            TargetImage = targetImage ?? throw new ArgumentNullException(nameof(targetImage));
            Recompiler = recompiler ?? throw new ArgumentNullException(nameof(recompiler));
            ExportResolver = exportResolver ?? throw new ArgumentNullException(nameof(exportResolver));
            ReferenceImporter = new ReferenceImporter(targetImage);
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

        public MetadataImage TargetImage
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

        public IGenericContext GenericContext => _genericContexts.Peek();
        
        public void EnterMember(IMemberReference member)
        {
            IGenericArgumentsProvider type = null;
            IGenericArgumentsProvider method = null;

            if (member is TypeSpecification typeSpec)
            {
                type = typeSpec.Signature as GenericInstanceTypeSignature;
            }
            else if (member is IMemberReference memberRef)
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
            ICallableMemberReference method, 
            IList<ILExpression> arguments,
            VMECallOpCode opCode,
            ITypeDescriptor constrainedType = null)
        {
            var methodSig = (MethodSignature) method.Signature;
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
                        argumentType = methodSig.Parameters[i - 1].ParameterType;
                    }
                }
                else
                {
                    // Static method invocation.
                    argumentType = methodSig.Parameters[i].ParameterType;
                }

                cilArgument.ExpectedType = argumentType.InstantiateGenericTypes(GenericContext);
                result.Add(cilArgument);
            }
            return result;
        }
        
    }
}