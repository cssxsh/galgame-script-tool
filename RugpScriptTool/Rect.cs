using ImageMagick;

namespace rUGP
{
    public readonly struct Rect
    {
        public readonly ushort X;
        public readonly ushort Y;
        public readonly ushort W;
        public readonly ushort H;

        public Rect(ushort x, ushort y, ushort w, ushort h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        public MagickGeometry ToMagickGeometry()
        {
            return new MagickGeometry(X, Y, W, H);
        }
    }
}