using System;
using System.IO;
using ImageMagick;

namespace rUGP
{
    // ReSharper disable MemberCanBeProtected.Global
    public class CRip : CSbm
    {
        public Rect ValidateRect { protected set; get; }

        protected uint Flags;

        protected uint X3C;

        protected byte[] Compressed;

        public CRip(string name, byte[] bytes)
        {
            Name = name;

            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            Version = reader.ReadUInt32();
            var x = reader.ReadUInt16();
            var y = reader.ReadUInt16();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            var w = reader.ReadUInt16();
            var h = reader.ReadUInt16();
            Flags = reader.ReadUInt32();
            ValidateRect = new Rect(x, y, w, h);
            var size = reader.ReadInt32();
            X3C = reader.ReadUInt32();
            Compressed = reader.ReadBytes(size);
        }

        public override MagickImage ToImage()
        {
            var settings = new MagickReadSettings
            {
                Width = Width,
                Height = Height,
                Format = (Flags & 0xFF) switch
                {
                    0x01 => MagickFormat.Gray,
                    0x02 => MagickFormat.Bgra, // BGRx
                    0x03 => MagickFormat.Bgra,
                    _ => throw new FormatException($"unsupported flags: {Flags:X8}"),
                },
                Depth = 8,
                Page = ValidateRect.ToMagickGeometry()
            };
            var data = (Flags & 0xFF) switch
            {
                0x01 => UnCompressSia(),
                0x02 => ((Flags >> 0x10) & 0xFF) switch
                {
                    0x01 => throw new NotSupportedException("?UnCompressRgb1@CRip@@QBEXPAUIS5i@@@Z"),
                    0x02 => throw new NotSupportedException("?UnCompressRgb2@CRip@@QBEXPAUIS5i@@@Z"),
                    0x03 => throw new NotSupportedException("?UnCompressRgb2@CRip@@QBEXPAUIS5i@@@Z"),
                    _ => null
                },
                0x03 => ((Flags >> 0x10) & 0xFF) switch
                {
                    0x02 => throw new NotSupportedException("?UnCompressRgba1@CRip@@QBEXPAUIS5i@@PBUtagRBDY@@1W4tagRipRop@1@@Z"),
                    _ => null
                },
                _ => null
            } ?? throw new FormatException($"unsupported flags: {Flags:X8}");

            var image = new MagickImage(data, settings);
            image.Quality = 100;
            return image;
        }

        // ?UnCompressSia@CRip@@QBEXV?$CRef@VCImgArea@@VCImgArea_ome@@VTImgArea@@@@@Z
        private byte[] UnCompressSia()
        {
            var output = new byte[Width * Height * 1];
            var src = 0;
            var stride = Width * 1;
            for (var y = 0; y < Height; y++)
            {
                var dst = y * stride;
                var color = (byte)0;
                for (var x = 0; x < Width; color = x < Width && src < Compressed.Length ? Compressed[src++] : color)
                {
                    var count = Compressed[src++];
                    for (var i = 0; i < count; i++) output[dst++] = color;
                    x += count;
                }
            }

            return output;
        }

        protected CRip(string name)
        {
            Name = name;
        }

        protected bool ReadBit(ref int position)
        {
            return (Compressed[position >> 0x03] & (0x80 >> (position++ & 0x07))) != 0;
        }

        protected int ReadInt(ref int position)
        {
            var x = 1;
            while (ReadBit(ref position)) x = (x << 0x01) | (ReadBit(ref position) ? 0x01 : 0x00);
            return x;
        }

        protected int ReadSigned(ref int position)
        {
            return ReadBit(ref position) ? -ReadInt(ref position) : ReadInt(ref position);
        }
    }
}