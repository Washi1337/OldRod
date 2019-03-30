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

namespace OldRod.Core.Recompiler
{
    public class RecompilerContext
    {
        public RecompilerContext(CilMethodBody methodBody, MetadataImage targetImage,
            ILToCilRecompiler recompiler, IVMFunctionResolver exportResolver)
        {
            MethodBody = methodBody ?? throw new ArgumentNullException(nameof(methodBody));
            TargetImage = targetImage ?? throw new ArgumentNullException(nameof(targetImage));
            Recompiler = recompiler ?? throw new ArgumentNullException(nameof(recompiler));
            ExportResolver = exportResolver ?? throw new ArgumentNullException(nameof(exportResolver));
            ReferenceImporter = new ReferenceImporter(targetImage);
        }

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
        
        public IDictionary<ILVariable, CilVariable> Variables
        {
            get;
        } = new Dictionary<ILVariable, CilVariable>();

        public IDictionary<ILParameter, ParameterSignature> Parameters
        {
            get;
        } = new Dictionary<ILParameter, ParameterSignature>();
        
        public CilVariable FlagVariable
        {
            get;
            set;
        }
        
        public  IList<CilExpression> RecompileCallArguments(ICallableMemberReference method, IList<ILExpression> arguments)
        {
            var methodSig = (MethodSignature) method.Signature;
            var result = new List<CilExpression>();
            
            // Emit arguments.
            for (var i = 0; i < arguments.Count; i++)
            {
                var cilArgument = (CilExpression) arguments[i].AcceptVisitor(Recompiler);

                var argumentType = methodSig.HasThis
                    ? i == 0
                        ? (ITypeDescriptor) method.DeclaringType
                        : methodSig.Parameters[i - 1].ParameterType
                    : methodSig.Parameters[i].ParameterType;

                result.Add(cilArgument.EnsureIsType(ReferenceImporter.ImportType(argumentType.ToTypeDefOrRef())));
            }
            return result;
        }
        
    }
}