using AsmResolver;

namespace OldRod.Core.Architecture
{
    public static class Utils
    {
        
        public static uint ReadCompressedUInt(IBinaryStreamReader reader)
        {
            uint num = 0;
            var shift = 0;
            byte current;
            do
            {
                current = reader.ReadByte();
                num |= (current & 0x7fu) << shift;
                shift += 7;
            } while((current & 0x80) != 0);
            return num;
        }

        public static uint FromCodedToken(uint codedToken)
        {
            var rid = codedToken >> 3;
            switch(codedToken & 7)
            {
                case 1:
                    return rid | 0x02000000;
                case 2:
                    return rid | 0x01000000;
                case 3:
                    return rid | 0x1b000000;
                case 4:
                    return rid | 0x0a000000;
                case 5:
                    return rid | 0x06000000;
                case 6:
                    return rid | 0x04000000;
                case 7:
                    return rid | 0x2b000000;
            }
            return rid;
        }

    }
}