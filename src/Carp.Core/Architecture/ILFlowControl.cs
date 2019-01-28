namespace Carp.Core.Architecture
{
    public enum ILFlowControl : byte
    {
        Next,
        Jump,
        ConditionalJump,
        Call,
        Return
    }
}