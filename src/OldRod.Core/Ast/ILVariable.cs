namespace OldRod.Core.Ast
{
    public class ILVariable
    {
        public ILVariable(string name)
        {
            Name = name;
        }
        
        public string Name
        {
            get;
            set;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}