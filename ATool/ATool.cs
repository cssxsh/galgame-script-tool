using System;

namespace ATool
{
    public static class ATool
    {
        public static byte[] TrimEnd(this byte[] source)
        {
            var target = (byte[])source.Clone();
            for (var i = 0; i < source.Length; i++)
            {
                if (source[i] != 0x00) continue;
                Array.Resize(ref target, i);
                break;
            }

            return target;
        }
    }
}