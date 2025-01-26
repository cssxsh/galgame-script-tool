using ImageMagick;

namespace rUGP
{
    // ReSharper disable MemberCanBeProtected.Global
    public class CSbm
    {
        public string Name { protected set; get; }

        public uint Version { protected set; get; }

        public ushort Width { protected set; get; }
        public ushort Height { protected set; get; }

        public virtual MagickImage ToImage()
        {
            return null;
        }
    }
}