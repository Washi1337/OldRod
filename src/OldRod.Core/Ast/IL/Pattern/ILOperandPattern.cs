namespace OldRod.Core.Ast.IL.Pattern
{
    public class ILOperandPattern
    {
        public static readonly ILOperandPattern Any = new ILOperandAnyPattern();
        
        public static readonly ILOperandPattern Null = new ILOperandPattern(null);
        
        private sealed class ILOperandAnyPattern : ILOperandPattern
        {
            public ILOperandAnyPattern() 
                : base(null)
            {
            }
            
            public override bool Match(object operand)
            {
                return true;
            }

            public override string ToString()
            {
                return "?";
            }
        }
        
        public ILOperandPattern(object operand)
        {
            Operand = operand;
        }

        public object Operand
        {
            get;
        }

        public virtual bool Match(object operand)
        {
            return Equals(Operand, operand);
        }

        public override string ToString()
        {
            return Operand == null ? "null" : Operand.ToString();
        }
    }
}