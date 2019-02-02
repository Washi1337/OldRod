namespace OldRod.Core.Architecture
{
    public enum VMECallOpCode : byte
    {
        ECALL_CALL,
        ECALL_CALLVIRT,
        ECALL_NEWOBJ,
        ECALL_CALLVIRT_CONSTRAINED
    }
}