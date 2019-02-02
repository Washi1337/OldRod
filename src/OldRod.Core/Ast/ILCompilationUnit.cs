using System.Collections.Generic;

namespace OldRod.Core.Ast
{
    public class ILCompilationUnit
    {
        private readonly IDictionary<string, ILVariable> _variables = new Dictionary<string, ILVariable>();

        public IList<ILStatement> Statements
        {
            get;
        } = new List<ILStatement>();

        public IEnumerable<ILVariable> GetVariables()
        {
            return _variables.Values;
        }

        public ILVariable GetOrCreateVariable(string name)
        {
            if (!_variables.TryGetValue(name, out var variable))
                _variables.Add(name, variable = new ILVariable(name));
            return variable;
        }
    }
}