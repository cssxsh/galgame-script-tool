using System;

namespace TamaSoft
{
    internal static class TamaSoftSecret
    {
        public static void Handle(byte[] data, uint key)
        {
            var secret = new uint[] { 0x0123, 0x0234, 0x0345, 0x0456 };
            var table = new uint[0x0272];
            table[0x0270] = 0x0000_0001u;
            table[0x0271] = 0x0000_0001u;
            Init(table, secret);
            Update(table, key);
            for (var i = 0; i < data.Length; i++) data[i] ^= Mask(table);
        }

        private static void Init(uint[] table, uint[] secret)
        {
            Update(table, 0x012B_D6AAu);

            var count = 0x0270;
            count = Math.Max(secret.Length, count);
            var p = 0x0000u;
            var q = 0x0001u;
            while (count-- > 0x0000)
            {
                var t = table[q - 0x0001u] >> 0x1E;
                t ^= table[q - 0x0001u];
                t *= 0x0019_660Du;
                t ^= table[q];
                table[q] = p + secret[p] + t;

                p = (p + 0x0001u) % (uint)secret.Length;
                q++;
                if (q < 0x0270u) continue;
                table[0x0000u] = table[0x026Fu];
                q = 0x0001u;
            }

            count = 0x026F;
            while (count-- > 0x0000)
            {
                var t = table[q - 0x0001u] >> 0x1E;
                t ^= table[q - 0x0001u];
                t *= 0x5D58_8B65u;
                t ^= table[q];
                table[q] = t - q;

                q++;
                if (q < 0x0270u) continue;
                table[0x0000u] = table[0x026Fu];
                q = 0x0001u;
            }

            table[0x0270] = 0x0000_0001u;
            table[0x0271] = 0x0000_0001u;
            table[0x0000] = 0x8000_0000u;
        }

        private static byte Mask(uint[] table)
        {
            if (--table[0x0270] == 0x0000_0000u) Flush(table);
            var t = table[0x0270 - table[0x0270]];
            t = (t >> 0x0B) ^ t;
            t = ((t & 0xFF3A_58AD) << 0x07) ^ t;
            t = ((t & 0xFFFF_DF8C) << 0x0F) ^ t;

            return (byte)((t ^ (t >> 0x12)) >> 0x01);
        }

        private static void Flush(uint[] table)
        {
            if (table[0x0271] == 0x0000_0000u) Update(table, 0x0000_1571u);
            table[0x0270] = 0x0000_0270u;
            for (var i = 0x0000u; i < 0x0270u; i++)
            {
                var a = table[(i + 0x0001u) % 0x0270u];
                var b = table[(i + 0x018Du) % 0x0270u];
                var c = (table[i] ^ (a ^ table[i]) & 0x7FFF_FFFE) >> 0x01;
                table[i] = b ^ ((a & 0x01) != 0 ? 0x9908_B0DFu : 0x0000_0000u) ^ c;
            }
        }

        private static void Update(uint[] table, uint key)
        {
            table[0x0000] = key;
            for (var i = 0x0001u; i < 0x0270u; i++)
            {
                var t = table[i - 0x0001] >> 0x1E;
                t ^= table[i - 0x0001];
                t *= 0x6C07_8965;
                table[i] = i + t;
            }

            table[0x0270] = 0x0000_0001u;
            table[0x0271] = 0x0000_0001u;
        }
    }
}