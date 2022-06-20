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
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;

namespace OldRod.Core.Recompiler.Transform
{
    public class TypeHelper
    {
        private readonly ITypeDefOrRef _arrayType;
        private readonly ITypeDefOrRef _objectType;

        private readonly IList<TypeSignature> _signedIntegralTypes;
        private readonly IList<TypeSignature> _unsignedIntegralTypes;
        private readonly IList<TypeSignature> _integralTypes;

        public TypeHelper(ReferenceImporter importer)
        {
            var ownerModule = importer.TargetModule;
            var factory = ownerModule.CorLibTypeFactory;
            var scope = ownerModule.CorLibTypeFactory.CorLibScope;

            _arrayType = new TypeReference(ownerModule, scope, "System", "Array");
            _objectType = new TypeReference(ownerModule, scope, "System", "Object");

            _signedIntegralTypes = new TypeSignature[]
            {
                factory.SByte,
                factory.Int16,
                factory.Int32,
                factory.IntPtr,
                factory.Int64,
            };
            
            _unsignedIntegralTypes = new TypeSignature[]
            {
                factory.Byte,
                factory.UInt16,
                factory.UInt32,
                factory.UIntPtr,
                factory.UInt64,
            };

            _integralTypes = new TypeSignature[]
            {
                factory.SByte,
                factory.Byte,
                factory.Int16,
                factory.UInt16,
                factory.Int32,
                factory.UInt32,
                factory.IntPtr,
                factory.UIntPtr,
                factory.Int64,
                factory.UInt64,
            };
        }
        
        public IList<ITypeDescriptor> GetTypeHierarchy(ITypeDescriptor type)
        {
            var result = new List<ITypeDescriptor>();
            
            TypeSignature typeSig;
            switch (type)
            {
                // The base type of an array type signature is System.Array, so it needs a special case. 
                // Get the type hierarchy of System.Array and then append the original array type sig.
                case ArrayTypeSignature _:
                case SzArrayTypeSignature _:
                    result.AddRange(GetTypeHierarchy(_arrayType));
                    result.Add(type);
                    return result;
                
                case ByReferenceTypeSignature byRef:
                    result.AddRange(GetTypeHierarchy(byRef.BaseType));
//                    result.Add(byRef);
                    return result;
                
                // Type specification's Resolve method resolves the underlying element type.
                // We therefore need a special case here, to get the type hierarchy of the embedded signature first.
                case TypeSpecification typeSpec:
                    result.AddRange(GetTypeHierarchy(typeSpec.Signature));
                    result.Add(typeSpec);
                    return result;
                
                case GenericParameterSignature genericParam:
                    // TODO: Resolve to actual generic parameter type.
                    result.Add(_objectType);
                    return result;
                
                // No type means no hierarchy.
                case null:
                    return Array.Empty<ITypeDescriptor>();
                
                default:
                    typeSig = type.ToTypeSignature();
                    break;
            }
            
            var genericContext = new GenericContext(null, null);
            
            while (typeSig != null)
            {
                if (typeSig is GenericInstanceTypeSignature genericInstance)
                    genericContext = new GenericContext(genericInstance, null);

                result.Add(typeSig);

                var typeDef = typeSig.ToTypeDefOrRef().Resolve();
                if (typeDef is null)
                {
                    throw new ArgumentException(
                        $"Could not resolve type {typeSig.FullName} in {typeSig.Scope.GetAssembly()}.");
                }

                if (typeDef.IsEnum)
                    typeSig = typeDef.GetEnumUnderlyingType();
                else if (typeDef.IsInterface && typeDef.BaseType is null)
                    typeSig = _objectType.ToTypeSignature();
                else
                    typeSig = typeDef.BaseType?.ToTypeSignature().InstantiateGenericTypes(genericContext);
            }

            result.Reverse();
            return result;
        }

        public bool IsIntegralType(ITypeDescriptor type)
        {
            return _integralTypes.Any(x => type.IsTypeOf(x.Namespace, x.Name));
        }
        
        public bool IsOnlyIntegral(IEnumerable<ITypeDescriptor> types)
        {
            return types.All(IsIntegralType);
        }

        public TypeSignature GetBiggestIntegralType(IEnumerable<ITypeDescriptor> types)
        {
            TypeSignature biggest = null;
            int biggestIndex = 0;
            
            foreach (var type in types)
            {
                int index = 0;
                for (index = 0; index < _integralTypes.Count; index++)
                {
                    if (_integralTypes[index].IsTypeOf(type.Namespace, type.Name))
                        break;
                }

                if (index > biggestIndex && index < _integralTypes.Count)
                {
                    biggest = _integralTypes[index];
                    biggestIndex = index;
                }
            }

            return biggest;
        }
        
        public ITypeDescriptor GetCommonBaseType(ICollection<ITypeDescriptor> types)
        {
            if (types.Count == 1)
                return types.First();
            
            if (IsOnlyIntegral(types))
                return GetBiggestIntegralType(types);

            // Strategy:
            // Get each type hierarchy, and walk from least specific (System.Object) to most specific type.
            // Break when there is a difference between two type hierarchies. This is a branch in the
            // total type hierarchy graph. 
            
            // TODO: For now we remove interfaces from the list to increase the chance of finding a more specific
            //       common type. This can be improved.
            
            // Obtain all base types for all types.
            var hierarchies = types
                .Where(t => !t.Resolve().IsInterface) 
                .Select(GetTypeHierarchy).ToList();
            if (hierarchies.Count == 0)
                return _objectType;
            
            ITypeDescriptor commonType = _objectType;

            int currentTypeIndex = 0;
            while (hierarchies.Count > 0)
            {
                ITypeDescriptor nextType = null;

                for (int i = 0; i < hierarchies.Count; i++)
                {
                    var hierarchy = hierarchies[i];
                    if (currentTypeIndex >= hierarchy.Count)
                    {
                        // Hierarchy is out of types. We can safely ignore this hierarchy any further
                        // since up to this point, this hierarchy has been exactly the same as the other hierarchies. 
                        hierarchies.RemoveAt(i);
                        i--;
                    }
                    else if (nextType == null)
                    {
                        nextType = hierarchy[currentTypeIndex];
                    }
                    else
                    {
                        // Check if the current hierarchy has branched from the other hierarchies.
                        if (hierarchy[currentTypeIndex].FullName != nextType.FullName)
                            return commonType;
                    }
                }

                if (nextType == null)
                    return commonType;
                
                commonType = nextType;
                currentTypeIndex++;
            }

            return commonType;
        }

        public bool IsAssignableTo(ITypeDescriptor from, ITypeDescriptor to)
        {
            if (to == null
                || from.FullName == to.FullName
                || from.IsTypeOf("System", "Int32") && to.IsTypeOf("System", "Boolean"))
            {
                return true;
            }

            if (from.IsValueType != to.IsValueType)
                return false;

            var typeHierarchy = GetTypeHierarchy(from);
            return typeHierarchy.Any(x => x.FullName == to.FullName);
        }
    }
}