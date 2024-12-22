using System.Collections.Generic;
using System.Linq;

namespace ATool
{
    public static class Hash
    {
        #region CRC

        public static uint[] CrcTable(uint polynomial, int width)
        {
            var table = new uint[0x0100];
            for (var n = 0x00u; n < 0x0100; n++)
            {
                var c = n << (width - 0x08);
                for (var k = 0x00; k < 0x08; k++)
                {
                    c = (c << 1) ^ (0 != (c & 0x8000_0000u) ? polynomial : 0x0000_0000);
                }

                table[n] = c;
            }

            return table;
        }

        public static uint Crc32(this IEnumerable<byte> source, uint init = 0xFFFF_FFFF)
        {
            var table = CrcTable(0x04C1_1DB7, 0x20);
            return source.Aggregate(init, (value, b) => table[(value >> 0x18) ^ b] ^ (value << 0x08));
        }

        #endregion
    }
}