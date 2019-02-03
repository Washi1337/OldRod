using System;
using System.Collections.Generic;
using AsmResolver.Net;
using AsmResolver.Net.Cts;
using OldRod.Core.Architecture;

namespace OldRod.Core.Disassembly.Inference
{
    public class TypeTable
    {
        private readonly IDictionary<ILStackBehaviour, ITypeDescriptor[]> _argumentTypes;
        private readonly IDictionary<ILStackBehaviour, ITypeDescriptor> _resultTypes;

        public TypeTable(MetadataImage image)
        {
            _argumentTypes = new Dictionary<ILStackBehaviour, ITypeDescriptor[]>
            {
                [ILStackBehaviour.None] = Array.Empty<ITypeDescriptor>(),
                [ILStackBehaviour.PopRegister] = new ITypeDescriptor[] {image.TypeSystem.Object},
                [ILStackBehaviour.PopPtr] = new ITypeDescriptor[] {image.TypeSystem.IntPtr},
                [ILStackBehaviour.PopByte] = new ITypeDescriptor[] {image.TypeSystem.Byte},
                [ILStackBehaviour.PopWord] = new ITypeDescriptor[] {image.TypeSystem.UInt16},
                [ILStackBehaviour.PopDword] = new ITypeDescriptor[] {image.TypeSystem.UInt32},
                [ILStackBehaviour.PopQword] = new ITypeDescriptor[] {image.TypeSystem.UInt64},
                [ILStackBehaviour.PopReal32] = new ITypeDescriptor[] {image.TypeSystem.Single},
                [ILStackBehaviour.PopReal64] = new ITypeDescriptor[] {image.TypeSystem.Double},
                [ILStackBehaviour.PopDword_PopDword] = new ITypeDescriptor[] {image.TypeSystem.UInt32, image.TypeSystem.UInt32},
                [ILStackBehaviour.PopQword_PopQword] = new ITypeDescriptor[] {image.TypeSystem.UInt64, image.TypeSystem.UInt64},
                [ILStackBehaviour.PopObject_PopObject] = new ITypeDescriptor[] {image.TypeSystem.Object, image.TypeSystem.Object},
                [ILStackBehaviour.PopReal32_PopReal32] = new ITypeDescriptor[] {image.TypeSystem.Single, image.TypeSystem.Single},
                [ILStackBehaviour.PopReal64_PopReal64] = new ITypeDescriptor[] {image.TypeSystem.Double, image.TypeSystem.Double},
                [ILStackBehaviour.PopPtr_PopPtr] = new ITypeDescriptor[] {image.TypeSystem.IntPtr, image.TypeSystem.IntPtr},
                [ILStackBehaviour.PopPtr_PopByte] = new ITypeDescriptor[] {image.TypeSystem.IntPtr, image.TypeSystem.Byte},
                [ILStackBehaviour.PopPtr_PopWord] = new ITypeDescriptor[] {image.TypeSystem.IntPtr, image.TypeSystem.UInt16},
                [ILStackBehaviour.PopPtr_PopDword] = new ITypeDescriptor[] {image.TypeSystem.IntPtr, image.TypeSystem.UInt32},
                [ILStackBehaviour.PopPtr_PopQword] = new ITypeDescriptor[] {image.TypeSystem.IntPtr, image.TypeSystem.UInt64},
                [ILStackBehaviour.PopPtr_PopObject] = new ITypeDescriptor[] {image.TypeSystem.IntPtr, image.TypeSystem.Object},
            };

            _resultTypes = new Dictionary<ILStackBehaviour, ITypeDescriptor>
            {
                [ILStackBehaviour.PushPtr] = image.TypeSystem.IntPtr,
                [ILStackBehaviour.PushByte] = image.TypeSystem.Byte,
                [ILStackBehaviour.PushWord] = image.TypeSystem.UInt16,
                [ILStackBehaviour.PushDword] = image.TypeSystem.UInt32,
                [ILStackBehaviour.PushQword] = image.TypeSystem.UInt64,
                [ILStackBehaviour.PushReal32] = image.TypeSystem.Single,
                [ILStackBehaviour.PushReal64] = image.TypeSystem.Double,
                [ILStackBehaviour.PushObject] = image.TypeSystem.Object,
            };
        }

        public ITypeDescriptor GetArgumentType(ILStackBehaviour popBehaviour, int argumentIndex)
        {
            return _argumentTypes[popBehaviour][argumentIndex];
        }

        public ITypeDescriptor GetResultType(ILStackBehaviour pushBehaviour)
        {
            return _resultTypes[pushBehaviour];
        }

    }
}