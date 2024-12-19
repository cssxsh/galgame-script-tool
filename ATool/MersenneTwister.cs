namespace ATool
{
    // from https://morkt.github.io/GARbro
    public class MersenneTwister
    {
        private const int StateLength = 624;
        private const int StateM = 397;
        private const uint MatrixA = 0x9908_B0DF;
        private const uint SignMask = 0x8000_0000;
        private const uint LowerMask = 0x7FFF_FFFF;
        private const uint TemperingMaskB = 0x9D2C_5680;
        private const uint TemperingMaskC = 0xEFC6_0000;

        private readonly uint[] _mt = new uint[StateLength];
        private int _mti = StateLength;

        public MersenneTwister(uint seed)
        {
            SRand(seed);
        }

        public void SRand(uint seed)
        {
            for (_mti = 0; _mti < _mt.Length; ++_mti)
            {
                uint upper = seed & 0xffff0000;
                seed = 69069 * seed + 1;
                _mt[_mti] = upper | (seed & 0xffff0000) >> 16;
                seed = 69069 * seed + 1;
            }
        }

        private readonly uint[] _mag01 = { 0, MatrixA };

        public uint Rand()
        {
            uint y;

            if (_mti >= StateLength)
            {
                int kk;
                for (kk = 0; kk < StateLength - StateM; kk++)
                {
                    y = (_mt[kk] & SignMask) | (_mt[kk + 1] & LowerMask);
                    _mt[kk] = _mt[kk + StateM] ^ (y >> 1) ^ _mag01[y & 1];
                }

                for (; kk < StateLength - 1; kk++)
                {
                    y = (_mt[kk] & SignMask) | (_mt[kk + 1] & LowerMask);
                    _mt[kk] = _mt[kk + StateM - StateLength] ^ (y >> 1) ^ _mag01[y & 1];
                }

                y = (_mt[StateLength - 1] & SignMask) | (_mt[0] & LowerMask);
                _mt[StateLength - 1] = _mt[StateM - 1] ^ (y >> 1) ^ _mag01[y & 1];

                _mti = 0;
            }

            y = _mt[_mti++];
            y ^= y >> 11;
            y ^= (y << 7) & TemperingMaskB;
            y ^= (y << 15) & TemperingMaskC;
            y ^= y >> 18;

            return y;
        }
    }
}