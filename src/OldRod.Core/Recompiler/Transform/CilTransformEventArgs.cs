using System;

namespace OldRod.Core.Recompiler.Transform
{
    public class CilTransformEventArgs : EventArgs
    {
        public CilTransformEventArgs(ICilAstTransform transform)
        {
            Transform = transform;
        }
        
        public ICilAstTransform Transform
        {
            get;
        }
    }
}