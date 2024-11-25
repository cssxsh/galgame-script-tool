using System;

namespace BGI
{
    internal struct HuffmanCode : IComparable<HuffmanCode>
    {
        public ushort Code;
        public ushort Depth;

        public int CompareTo(HuffmanCode other)
        {
            return Depth == other.Depth
                ? Code - other.Code
                : Depth - other.Depth;
        }
    }
}