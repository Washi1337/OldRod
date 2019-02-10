namespace OldRod.Core.Ast
{
    public interface IAstNode
    {
        IAstNode Parent
        {
            get;
            set;
        }
    }
}