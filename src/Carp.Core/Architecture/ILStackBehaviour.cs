namespace Carp.Core.Architecture
{
    public enum ILStackBehaviour : byte
    {
        None,
        
        Pop1,
        Pop2,
        PopVar,
        
        Push1,
        PushVar
    }
}