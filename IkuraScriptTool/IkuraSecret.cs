using System.Text;

namespace Ikura
{
    // form https://github.com/morkt/GARbro
    public static class IkuraSecret
    {
        public static void Handle(byte[] data, byte[] secret)
        {
            var key = secret.CreateKey();
            for (var i = 0; i < data.Length; i++)
            {
                if (i % key.Length == 0x00) secret.UpdateKey(key, i / key.Length);
                data[i] ^= key[i % key.Length];
            }
        }

        private static byte[] CreateKey(this byte[] secret)
        {
            var length = new byte[0x02];
            for (var i = 0; i < length.Length; i++)
            {
                length[i] = EncodeHex((byte)(Chr2HexCode(secret[0x500 + i]) - Chr2HexCode(secret[0x100 + i])));
            }

            var key = new byte[Str2Hex(length)];
            for (var i = 0; i < key.Length; i++)
            {
                key[i] = EncodeHex((byte)(Chr2HexCode(secret[0x510 + i]) - Chr2HexCode(secret[0x110 + i])));
            }

            return key;
        }

        private static void UpdateKey(this byte[] secret, byte[] key, int index)
        {
            var p = (index & 0x3F) * 0x10;
            for (var i = 0; i < key.Length; i++)
            {
                key[i] = EncodeHex((byte)(Chr2HexCode(key[i]) + Chr2HexCode(secret[p + i])));
            }
        }

        private static byte EncodeHex(byte symbol)
        {
            var index = (sbyte)symbol;
            return HexEncodeMap[(index % 36 + 36) % 36];
        }

        private static int Str2Hex(byte[] str)
        {
            var hex = 0;
            for (var i = 0; i < str.Length; ++i)
            {
                hex |= Chr2Hex(str[i]) << ((str.Length - i - 1) << 2);
            }

            return hex;
        }

        private static byte Chr2HexCode(byte chr)
        {
            return HexTable[Chr2Hex(chr)];
        }

        private static byte Chr2Hex(byte chr)
        {
            byte hex;
            if (chr >= '0' && chr <= '9')
                hex = (byte)(chr - '0');
            else if (chr >= 'a' && chr <= 'z')
                hex = (byte)(chr - 'a' + 10);
            else if (chr >= 'A' && chr <= 'Z')
                hex = (byte)(chr - 'A' + 10);
            else
                hex = 0;
            return hex;
        }

        private static readonly byte[] HexEncodeMap = Encoding.ASCII.GetBytes("G5FXIL094MPRKWCJ3OEBVA7HQ2SU8Y6TZ1ND");

        private static readonly byte[] HexTable =
        {
            0x06, 0x21, 0x19, 0x10, 0x08, 0x01, 0x1E, 0x16, 0x1C, 0x07, 0x15, 0x13, 0x0E, 0x23, 0x12, 0x02,
            0x00, 0x17, 0x04, 0x0F, 0x0C, 0x05, 0x09, 0x22, 0x11, 0x0A, 0x18, 0x0B, 0x1A, 0x1F, 0x1B, 0x14,
            0x0D, 0x03, 0x1D, 0x20,
        };
    }
}