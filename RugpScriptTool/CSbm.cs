using System;
using ImageMagick;

namespace rUGP
{
    // ReSharper disable MemberCanBeProtected.Global
    public class CSbm : ISbm
    {
        public string Name { protected set; get; }

        public uint Version { protected set; get; }

        public ushort Width { protected set; get; }
        public ushort Height { protected set; get; }

        public virtual MagickImage ToImage()
        {
            throw new NotImplementedException("CSbm::ToImage");
        }

        public virtual void Merge(MagickImage image)
        {
            throw new NotImplementedException("CSbm::Merge");
        }

        public virtual byte[] ToBytes()
        {
            throw new NotImplementedException("CSbm::ToBytes");
        }
    }
}