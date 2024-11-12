using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ATool;
using ImageMagick;

namespace Will
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable MemberCanBePrivate.Global
    public readonly struct WillBC
    {
        public readonly short OffsetX;

        public readonly short OffsetY;

        public readonly int Width;

        public readonly int Height;

        public readonly ushort BitsPerPixel;

        public readonly int Colors;

        public readonly byte[] Pixels;

        public readonly ushort Stride;

        public WillBC(byte[] bytes)
        {
            using var steam = new MemoryStream(bytes);
            using var reader = new BinaryReader(steam);
            // 0x00
            var header = Encoding.ASCII.GetString(reader.ReadBytes(0x02));
            if (header != "BC") throw new FormatException($"unsupported header: {header}");
            // 0x02 size
            var sizeOfBytes = reader.ReadUInt32();
            Debug.WriteLine($"0x02: {sizeOfBytes:X8}");
            // 0x06
            OffsetX = reader.ReadInt16();
            // 0x08
            OffsetY = reader.ReadInt16();
            // 0x0A always 0x00000036
            var data = reader.ReadUInt32();
            // 0x0E always 0x00000028
            var x0E = reader.ReadUInt32();
            Debug.WriteLine($"0x0E: {x0E:X8}");
            // 0x12
            Width = reader.ReadInt32();
            // 0x16
            Height = reader.ReadInt32();
            // 0x1A always 0x0001
            var x1A = reader.ReadUInt16();
            Debug.WriteLine($"0x1A: {x1A:X4}");
            // 0x1C
            BitsPerPixel = reader.ReadUInt16();
            // 0x1E 0x00000003 or 0x00000000
            var x1E = reader.ReadUInt32();
            Debug.WriteLine($"0x1E: {x1E:X8}");
            // 0x22
            var countOfPixels = reader.ReadUInt32();
            Pixels = new byte[countOfPixels];

            steam.Position = 0x0000_0002E;
            Colors = reader.ReadInt32();
            if (Colors < 0) throw new FormatException($"unsupported colors: {Colors:X8}");

            steam.Position = data;
            var format = Encoding.ASCII.GetString(reader.ReadBytes(0x04));
            if (format != "TX04") throw new FormatException($"unsupported format: {format}");
            Stride = reader.ReadUInt16(); // eq Width * BBP / 8
            _ = reader.ReadUInt16(); // eq Height

            // Decompress form https://github.com/morkt/GARbro
            var dst = 0x0000_0000;
            dst += steam.Read(Pixels, dst, 0x02);
            while (dst < Pixels.Length && steam.Position < steam.Length)
            {
                var count = (int)reader.ReadByte();
                if (0xE0 == (count & 0xE0))
                {
                    count = Math.Min((count & 0x1F) + 0x01, Pixels.Length - dst);
                    dst += steam.Read(Pixels, dst, count);
                    continue;
                }

                var src = 0;
                switch (count & 0xC0)
                {
                    case 0x00:
                    {
                        var offset = (count >> 0x3) & 0x07;
                        count &= 0x7;
                        count = count != 0x07
                            ? count + 0x02
                            : reader.ReadByte();
                        src = dst - 0x01 - offset;
                    }
                        break;
                    case 0x40:
                    {
                        var offset = (count >> 0x2) & 0x0F;
                        count &= 0x3;
                        count = count != 0x03
                            ? count + 0x02
                            : reader.ReadByte();
                        src = dst - Stride + offset - 0x08;
                    }
                        break;
                    case 0xC0:
                    {
                        var offset = (count + (reader.ReadByte() << 0x05)) & 0x1F;
                        count = reader.ReadByte();
                        src = dst - 0x01 - offset;
                    }
                        break;
                    default:
                    {
                        var offset = (count >> 0x02) & 0x0F;
                        count &= 0x03;
                        count = count != 0x03
                            ? count + 0x02
                            : reader.ReadByte();
                        src = dst - Stride * 0x02 + offset - 0x08;
                    }
                        break;
                }

                count = Math.Min(count, Pixels.Length - dst);
                Pixels.CopyOverlapped(src, dst, count);
                dst += count;
            }

            var size = BitsPerPixel / 8;
            if (size <= 1) return;
            for (var y = 0; y < Height; y++)
            {
                for (var x = 1; x < Width; x++)
                {
                    var pos = y * Stride + x * size;
                    for (var i = 0; i < size; i++)
                    {
                        Pixels[pos + i] += Pixels[pos + i - size];
                    }
                }

                Array.Reverse(Pixels, y * Stride, Stride);
            }

            Array.Reverse(Pixels);
        }

        public MagickImage ToImage()
        {
            var format = BitsPerPixel switch
            {
                0x0020 => "BGRA",
                0x0018 => "BGR",
                _ => throw new FormatException($"unsupported bbp: {BitsPerPixel:X4}"),
            };
            var settings = new PixelReadSettings(Width, Height, StorageType.Char, format);
            var image = new MagickImage();
            image.ReadPixels(Pixels, settings);
            image.Page = new MagickGeometry(OffsetX, OffsetY, Width, Height);
            image.Quality = 100;
            return image;
        }
    }
}