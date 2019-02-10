using System.Collections.Generic;

namespace OldRod.Core.Ast.IL
{
    public interface IILArgumentsProvider
    {
        IList<ILExpression> Arguments
        {
            get;
        }
    }
}