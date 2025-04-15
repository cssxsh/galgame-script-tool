using ImageMagick;

namespace rUGP
{
    public interface ISbm
    {
        public ushort Width { get; }
        public ushort Height { get; }
        
        public MagickImage ToImage();

        public void Merge(MagickImage image);

        public byte[] ToBytes();
    }
}