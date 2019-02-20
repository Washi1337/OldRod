namespace OldRod.Core.Ast.IL.Pattern
{
    public class ILOperandPattern
    {
        public static ILOperandPattern Any() => new ILOperandAnyPattern();
        
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
            return Operand == operand;
        }
    }
}