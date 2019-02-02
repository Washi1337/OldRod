namespace OldRod.Core.Architecture
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