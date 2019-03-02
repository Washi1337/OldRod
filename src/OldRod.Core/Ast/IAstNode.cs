using System.Collections.Generic;

namespace OldRod.Core.Ast
{
    public interface IAstNode
    {
        IAstNode Parent
        {
            get;
            set;
        }

        IEnumerable<IAstNode> GetChildren();
    }
}