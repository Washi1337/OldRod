using System;
using System.Collections.Generic;
using AsmResolver.Net;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;

namespace OldRod.Core.Recompiler
{
    public class GenericContext
    {
        public GenericContext(IGenericArgumentsProvider type, IGenericArgumentsProvider method)
        {
            Type = type;
            Method = method;
        }        
        
        public IGenericArgumentsProvider Type
        {
            get;
        }

        public IGenericArgumentsProvider Method
        {
            get;
        }
        
        public ITypeDescriptor ResolveTypeArgument(GenericParameterSignature genericParameter)
        {
            IGenericArgumentsProvider provider;
            switch (genericParameter.ParameterType)
            {
                case GenericParameterType.Type:
                    provider = Type;
                    break;
                case GenericParameterType.Method:
                    provider = Method;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return provider.GenericArguments[genericParameter.Index];
        }

        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, {nameof(Method)}: {Method}";
        }
    }
    
}