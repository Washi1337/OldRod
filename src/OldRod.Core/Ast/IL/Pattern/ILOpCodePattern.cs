using System.Collections.Generic;
using System.Linq;
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.IL.Pattern
{
    public class ILOpCodePattern
    {
        public static implicit operator ILOpCodePattern(ILCode code)
        {
            return new ILOpCodePattern(code);
        } 
        
        public static ILOpCodePattern Any() => new ILOpCodeAnyPattern();
        
        private sealed class ILOpCodeAnyPattern : ILOpCodePattern
        {
            public override bool Match(ILCode code)
            {
                return true;
            }

            public override string ToString()
            {
                return "?";
            }
        }
        
        public ILOpCodePattern(params ILCode[] opCode)
        {
            OpCodes = new HashSet<ILCode>(opCode);
        }
        
        public ISet<ILCode> OpCodes
        {
            get;
        }
        
        public virtual bool Match(ILCode code)
        {
            return OpCodes.Contains(code);
        }

        public override string ToString()
        {
            return OpCodes.Count == 1 
                ? OpCodes.First().ToString() 
                : $"({string.Join("|", OpCodes)})";
        }
    }
}