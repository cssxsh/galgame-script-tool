using System;
using System.Collections.Generic;
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

                var src = dst;
                switch (count & 0xC0)
                {
                    case 0x00:
                    {
                        var offset = (count >> 0x03) & 0x07;
                        count &= 0x07;
                        count = count != 0x07
                            ? count + 0x02
                            : reader.ReadByte();
                        src -= 0x01 + offset;
                    }
                        break;
                    case 0x40:
                    {
                        var offset = (count >> 0x02) & 0x0F;
                        count &= 0x03;
                        count = count != 0x03
                            ? count + 0x02
                            : reader.ReadByte();
                        src -= 0x08 + Stride - offset;
                    }
                        break;
                    case 0xC0:
                    {
                        var offset = (count + (reader.ReadByte() << 0x05)) & 0x1F;
                        count = reader.ReadByte();
                        src -= 0x01 + offset;
                    }
                        break;
                    default:
                    {
                        var offset = (count >> 0x02) & 0x0F;
                        count &= 0x03;
                        count = count != 0x03
                            ? count + 0x02
                            : reader.ReadByte();
                        src -= 0x08 + Stride * 0x02 - offset;
                    }
                        break;
                }

                count = Math.Min(count, Pixels.Length - dst);
                Pixels.CopyOverlapped(src, dst, count);
                dst += count;
            }

            var depth = BitsPerPixel / 8;
            if (depth <= 1) return;
            for (var y = 0; y < Height; y++)
            {
                for (var x = 1; x < Width; x++)
                {
                    var pos = y * Stride + x * depth;
                    for (var i = 0; i < depth; i++)
                    {
                        Pixels[pos + i] += Pixels[pos + i - depth];
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

        public byte[] ToBytes()
        {
            var buffer = new byte[0x0000_003E];
            using var stream = new MemoryStream(buffer);
            using var writer = new BinaryWriter(stream);
            writer.Write(Encoding.ASCII.GetBytes("BC"));
            writer.Write(0x0000_00000);
            writer.Write(OffsetX);
            writer.Write(OffsetY);
            writer.Write(0x0000_0036);
            writer.Write(0x0000_0028);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write((ushort)0x0001);
            writer.Write(BitsPerPixel);
            writer.Write(OffsetX == 0 && OffsetY == 0 ? 0x0000_0000 : 0x0000_0003);
            writer.Write(Pixels.Length);

            stream.Position = 0x0000_0002E;
            writer.Write(Colors);

            stream.Position = 0x0000_00036;
            writer.Write(Encoding.ASCII.GetBytes("TX04"));
            writer.Write(Stride);
            writer.Write((ushort)Height);

            var pixels = (byte[])Pixels.Clone();
            var depth = BitsPerPixel switch
            {
                0x0020 => BitsPerPixel / 0x08,
                0x0018 => BitsPerPixel / 0x08,
                _ => throw new FormatException($"unsupported bbp: {BitsPerPixel:X4}"),
            };
            Array.Reverse(pixels);
            for (var y = 0; y < Height; y++)
            {
                Array.Reverse(pixels, y * Stride, Stride);
                for (var x = 1; x < Width; x++)
                {
                    var pos = y * Stride + (Width - x) * depth;
                    for (var i = 0; i < depth; i++)
                    {
                        pixels[pos + i] -= pixels[pos + i - depth];
                    }
                }
            }

            // var length = pixels.Length;
            // while (length > 0x02 && pixels[length - 0x01] == 0x00) length--;

            var stack = new Stack<byte>(pixels.Length);
            var result = Array.Empty<byte>();
            var dp = new Dictionary<int, int>();
            var stride = (int)Stride;
            Compress(pixels.Length);

            var size = buffer.Length + result.Length;
            stream.Position = 0x0000_0002;
            writer.Write(size);
            Array.Resize(ref buffer, size);
            result.CopyTo(buffer, 0x0000_003E);

            return buffer;

            void Compress(int limit)
            {
                if (limit == 0x02)
                {
                    stack.Push(pixels[0x01]);
                    stack.Push(pixels[0x00]);
                    if (result.Length == 0x00 || result.Length > stack.Count) result = stack.ToArray();
                    stack.Pop();
                    stack.Pop();
                    return;
                }

                var current = stack.Count;
                if (((result.Length + 0x3E) & 0x0F) == 0x00) return;
                var e0 = current == 0x00 || stack.Peek() < 0xE0 || stack.Peek() == 0xFF;

                for (var count = 0xFF; count > 0x01; count--)
                {
                    var dst = limit - count;
                    if (dst < 0x02) continue;
                    dp.TryGetValue(dst, out var prev);
                    if (prev == 0x00) prev = int.MaxValue;
                    if (prev <= current + (count > 0x08 ? 2 : 1)) continue;
                    // if (prev <= current + 1) continue;

                    var used = 0x00;

                    // 0x00
                    for (var offset = Math.Min(0x08, dst); offset > 0x00; offset--)
                    {
                        // if (used != 0 && used <= (count < 0x09 ? 0x01 : 0x02)) break;
                        var src = dst - offset;
                        var x = 0;
                        while (dst + x < pixels.Length && pixels[src + x % offset] == pixels[dst + x]) x++;
                        if (x < count) continue;
                        while (used-- > 0x00) stack.Pop();
                        var mask = 0x00;
                        mask |= (offset - 0x01) << 0x03;
                        if (count < 0x09)
                        {
                            used = 0x01;
                            mask |= count - 0x02;
                        }
                        else
                        {
                            used = 0x02;
                            mask |= 0x07;
                            stack.Push((byte)count);
                        }
                        
                        if ((mask & 0xC0) != 0x00) throw new Exception("...");
                        stack.Push((byte)mask);
                        break;
                    }

                    // 0x40
                    for (var offset = Math.Min(stride + 0x08, dst); offset > stride - 0x08; offset--)
                    {
                        // if (used != 0 && used <= (count < 0x05 ? 0x01 : 0x02)) break;
                        if (used != 0) break;
                        var src = dst - offset;
                        var x = 0;
                        while (dst + x < pixels.Length && pixels[src + x % offset] == pixels[dst + x]) x++;
                        if (x < count) continue;
                        while (used-- > 0x00) stack.Pop();
                        var mask = 0x40;
                        mask |= (stride + 0x08 - offset) << 0x02;
                        if (count < 0x05)
                        {
                            used = 0x01;
                            mask |= count - 0x02;
                        }
                        else
                        {
                            used = 0x02;
                            mask |= 0x03;
                            stack.Push((byte)count);
                        }

                        if ((mask & 0xC0) != 0x40) throw new Exception("...");
                        stack.Push((byte)mask);
                        break;
                    }

                    // 0x80
                    for (var offset = Math.Min(stride * 0x02 + 0x08, dst); offset > stride * 0x02 - 0x08; offset--)
                    {
                        // if (used != 0 && used <= (count < 0x05 ? 0x01 : 0x02)) break;
                        if (used != 0) break;
                        var src = dst - offset;
                        var x = 0;
                        while (dst + x < pixels.Length && pixels[src + x % offset] == pixels[dst + x]) x++;
                        if (x < count) continue;
                        while (used-- > 0x00) stack.Pop();
                        var mask = 0x80;
                        mask |= (stride * 0x02 + 0x08 - offset) << 0x02;
                        if (count < 0x05)
                        {
                            used = 0x01;
                            mask |= count - 0x02;
                        }
                        else
                        {
                            used = 0x02;
                            mask |= 0x03;
                            stack.Push((byte)count);
                        }

                        if ((mask & 0xC0) != 0x80) throw new Exception("...");
                        stack.Push((byte)mask);
                        break;
                    }

                    // 0xC0
                    for (var offset = Math.Min(0x20, dst); offset > 0x08; offset--)
                    {
                        // if (used != 0 && used <= 0x03) break;
                        if (used != 0) break;
                        var src = dst - offset;
                        var x = 0;
                        while (dst + x < pixels.Length && pixels[src + x % offset] == pixels[dst + x]) x++;
                        if (x < count) continue;
                        while (used-- > 0x00) stack.Pop();
                        var mask = 0xC0;
                        mask |= offset - 0x01;
                        used = 0x03;
                        if ((mask & 0xC0) != 0xC0) throw new Exception("...");
                        stack.Push((byte)count);
                        stack.Push(0);
                        stack.Push((byte)mask);
                        break;
                    }

                    // 0xE0
                    if (e0 && count < 0x03 && used == 0)
                    {
                        var mask = 0xE0;
                        mask |= count - 0x01;
                        for (var x = 0; x < count; x++) stack.Push(pixels[limit - x - 1]);
                        if ((mask & 0xE0) != 0xE0) throw new Exception("...");
                        stack.Push((byte)mask);
                        used = count + 1;
                    }

                    if (current + used != stack.Count) throw new Exception("...");
                    if (used == 0x00) continue;
                    if (prev > stack.Count)
                    {
                        Compress(dst);
                        if (result.Length != 0x00) dp[dst] = stack.Count;
                    }

                    while (used-- > 0x00) stack.Pop();
                }

                // 0xE0
                if (!e0) return;
                for (var count = 0x03; count <= 0x20; count++)
                {
                    var dst = limit - count;
                    if (dst < 0x02) continue;
                    var used = count + 0x01;

                    dp.TryGetValue(dst, out var prev);
                    if (prev == 0x00) prev = int.MaxValue;
                    if (prev < current + used) continue;

                    var mask = 0xE0;
                    mask |= count - 0x01;
                    for (var x = 0; x < count; x++) stack.Push(pixels[limit - x - 1]);
                    if ((mask & 0xE0) != 0xE0) throw new Exception("...");
                    stack.Push((byte)mask);
                    if (current + used != stack.Count) throw new Exception("...");
                    Compress(dst);
                    if (result.Length != 0x00) dp[dst] = stack.Count;
                    while (used-- > 0x00) stack.Pop();
                }

                if (result.Length == 0x00) throw new Exception("...");
            }
        }
    }
}