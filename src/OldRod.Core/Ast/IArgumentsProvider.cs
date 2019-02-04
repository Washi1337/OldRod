using System.Collections.Generic;

namespace OldRod.Core.Ast
{
    public interface IArgumentsProvider
    {
        IList<ILExpression> Arguments
        {
            get;
        }
    }
}