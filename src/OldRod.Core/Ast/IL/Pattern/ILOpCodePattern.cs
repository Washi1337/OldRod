using System.Collections.Generic;
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL.Pattern
{
    public class ILOpCodePattern
    {
        public static ILOpCodePattern Any() => new ILOpCodeAnyPattern();
        
        private sealed class ILOpCodeAnyPattern : ILOpCodePattern
        {
            public ILOpCodeAnyPattern() 
                : base()
            {
            }
            
            public override bool Match(ILCode code)
            {
                return true;
            }
        }
        
        public ILOpCodePattern(params ILCode[] opCode)
        {
            OpCode = new HashSet<ILCode>(opCode);
        }
        
        public ISet<ILCode> OpCode
        {
            get;
        }
        
        public virtual bool Match(ILCode code)
        {
            return OpCode.Contains(code);
        }
    }
}