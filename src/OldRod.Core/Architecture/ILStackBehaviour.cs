namespace OldRod.Core.Architecture
{
    public enum ILStackBehaviour : byte
    {
        None,
        
        PopRegister,
        PopPtr,
        PopByte,
        PopWord,
        PopDword,
        PopQword,
        PopReal32,
        PopReal64,
        PopDword_PopDword,
        PopQword_PopQword,
        PopObject_PopObject,
        PopReal32_PopReal32,
        PopReal64_PopReal64,
        PopPtr_PopPtr,
        PopPtr_PopObject,
        PopPtr_PopByte,
        PopPtr_PopWord,
        PopPtr_PopDword,
        PopPtr_PopQword,
        PopVar,
        
        PushPtr,
        PushByte,
        PushWord,
        PushDword,
        PushQword,
        PushReal32,
        PushReal64,
        PushObject,
        PushVar,
    }
}