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

        public readonly uint Width;

        public readonly uint Height;

        public readonly ushort BitsPerPixel;

        private readonly uint _x1E;

        public readonly int Colors;

        public readonly byte[] Pixels;

        public WillBC(byte[] bytes)
        {
            using var steam = new MemoryStream(bytes);
            using var reader = new BinaryReader(steam);
            // 0x00
            var header = Encoding.ASCII.GetString(reader.ReadBytes(0x02));
            if (header != "BC") throw new FormatException($"unsupported header: {header}");
            // 0x02 size
            var sizeOfBytes = reader.ReadUInt32();
            // 0x06
            OffsetX = reader.ReadInt16();
            // 0x08
            OffsetY = reader.ReadInt16();
            // 0x0A always 0x00000036
            var data = reader.ReadUInt32();
            // 0x0E always 0x00000028
            var x0E = reader.ReadUInt32();
            Debug.WriteLine($"BC:0E {x0E:X8}");
            // 0x12
            Width = reader.ReadUInt32();
            // 0x16
            Height = reader.ReadUInt32();
            // 0x1A always 0x0001
            var x1A = reader.ReadUInt16();
            Debug.WriteLine($"BC:1A {x1A:X4}");
            // 0x1C
            BitsPerPixel = reader.ReadUInt16();
            var depth = BitsPerPixel switch
            {
                0x0020 => BitsPerPixel / 0x08,
                0x0018 => BitsPerPixel / 0x08,
                _ => throw new FormatException($"unsupported bbp: {BitsPerPixel:X4}"),
            };
            // 0x1E 0x00000003 or 0x00000000
            _x1E = reader.ReadUInt32();
            // 0x22
            var sizeOfPixels = reader.ReadUInt32();
            Pixels = new byte[sizeOfPixels];

            steam.Position = 0x0000_0002E;
            Colors = reader.ReadInt32();
            if (Colors < 0) throw new FormatException($"unsupported colors: {Colors:X8}");

            steam.Position = data;
            var format = Encoding.ASCII.GetString(reader.ReadBytes(0x04));
            if (format != "TX04") throw new FormatException($"unsupported format: {format}");
            var stride = reader.ReadUInt16(); // eq Width * BBP / 8
            if (stride != Width * depth) throw new FormatException($"unsupported stride: {stride} != Width * depth");
            var lines = reader.ReadUInt16();
            if (lines != Height) throw new FormatException($"unsupported lines: {lines} != Height");

            // Decompress form https://github.com/morkt/GARbro
            var dst = 0x0000_0000;
            dst += steam.Read(Pixels, dst, 0x02);
            while (dst < Pixels.Length && steam.Position < sizeOfBytes)
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
                        src -= 0x08 + stride - offset;
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
                        src -= 0x08 + stride * 0x02 - offset;
                    }
                        break;
                }

                count = Math.Min(count, Pixels.Length - dst);
                Pixels.CopyOverlapped(src, dst, count);
                dst += count;
            }

            for (var y = 0; y < Height; y++)
            {
                for (var x = 1; x < Width; x++)
                {
                    var pos = y * stride + x * depth;
                    for (var i = 0; i < depth; i++)
                    {
                        Pixels[pos + i] += Pixels[pos + i - depth];
                    }
                }

                Array.Reverse(Pixels, y * stride, stride);
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

        public void Merge(MagickImage image)
        {
            if (image.Width != Width || image.Height != Height)
                throw new FormatException($"size no match: {image.Width}x{image.Height}");
            var format = BitsPerPixel switch
            {
                0x0020 => "BGRA",
                0x0018 => "BGR",
                _ => throw new FormatException($"unsupported bbp: {BitsPerPixel:X4}"),
            };
            var pixels = image.GetPixels().ToByteArray(format)
                         ?? throw new FormatException($"get pixels<{format}> fail!");
            if (pixels.Length != Pixels.Length)
                throw new FormatException($"unsupported pixels length: {pixels.Length}");
            Array.Copy(pixels, 0, Pixels, 0, pixels.Length);
        }

        public byte[] ToBytes()
        {
            var pixels = (byte[])Pixels.Clone();
            var depth = BitsPerPixel switch
            {
                0x0020 => BitsPerPixel / 0x08,
                0x0018 => BitsPerPixel / 0x08,
                _ => throw new FormatException($"unsupported bbp: {BitsPerPixel:X4}"),
            };
            var stride = (ushort)(Width * depth);
            Array.Reverse(pixels);
            for (var y = 0; y < Height; y++)
            {
                Array.Reverse(pixels, y * stride, stride);
                for (var x = 1; x < Width; x++)
                {
                    var pos = y * stride + (Width - x) * depth;
                    for (var i = 0; i < depth; i++)
                    {
                        pixels[pos + i] -= pixels[pos + i - depth];
                    }
                }
            }

            var length = pixels.Length;

            var stack = new Stack<CompressBlock>(pixels.Length);
            var result = Array.Empty<byte>();
            var dp = new Dictionary<int, int>();
            var capacity = 0;
            var next = false;
            while (stack.Count > 0 || !next)
            {
                if (length == 0x02)
                {
                    if (result.Length == 0x00 || result.Length > capacity)
                    {
                        result = Build();
                        Debug.WriteLine($"pixels: {pixels.Length:X4} => {result.Length:X4}");
                        if ((result.Length & 0x0F) == 0x00) break;
                    }

                    next = true;
                }

                if (!next)
                {
                    if (dp.TryGetValue(length, out var value) && value <= capacity)
                    {
                        next = true;
                        continue;
                    }

                    var block = Find(0xFF);
                    if (block.Count == 0x00)
                    {
                        next = true;
                        continue;
                    }

                    stack.Push(block);
                    length -= stack.Peek().Count;
                    capacity += stack.Peek().Size;
                    continue;
                }

                var current = stack.Pop();
                length += current.Count;
                capacity -= current.Size;
                if (current.Flag == 0xE0)
                {
                    var count = current.Count + 0x01;
                    if (count > 0x20 || length - count < 0x02)
                    {
                        if (!dp.TryGetValue(length, out var value) || value > capacity) dp[length] = capacity;
                        continue;
                    }

                    var block = new CompressBlock(0xE0, length - count, count);
                    stack.Push(block);
                }
                else
                {
                    var block = Find(current.Count - 1);
                    if (block.Count == 0x00) continue;
                    stack.Push(block);
                }

                next = false;
                length -= stack.Peek().Count;
                capacity += stack.Peek().Size;
            }

            var size = 0x0000_0040 + ((result.Length + 0x0F) & ~0x0F);
            var buffer = new byte[size];
            using var stream = new MemoryStream(buffer);
            using var writer = new BinaryWriter(stream);
            writer.Write(Encoding.ASCII.GetBytes("BC"));
            writer.Write(size);
            writer.Write(OffsetX);
            writer.Write(OffsetY);
            writer.Write(0x0000_0036);
            writer.Write(0x0000_0028);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write((ushort)0x0001);
            writer.Write(BitsPerPixel);
            writer.Write(_x1E);
            writer.Write(Pixels.Length);

            stream.Position = 0x0000_0002E;
            writer.Write(Colors);

            stream.Position = 0x0000_00036;
            writer.Write(Encoding.ASCII.GetBytes("TX04"));
            writer.Write(stride);
            writer.Write((ushort)Height);
            writer.Write(pixels[0]);
            writer.Write(pixels[1]);
            writer.Write(result);

            return buffer;

            CompressBlock Find(int limit)
            {
                for (var count = limit; count > 0x01; count--)
                {
                    var dst = length - count;
                    if (dst < 0x02) continue;

                    // 0x00
                    for (var offset = Math.Min(0x08, dst); offset > 0x00; offset--)
                    {
                        var src = dst - offset;
                        var x = 0;
                        while (dst + x < pixels.Length && pixels[src + x % offset] == pixels[dst + x]) x++;
                        if (x < count) continue;
                        // if (x != count && stack.Peek().Flag == 0x00) continue;
                        return new CompressBlock(0x00, offset, count);
                    }

                    // 0x40
                    for (var offset = Math.Min(stride + 0x08, dst); offset > stride - 0x08; offset--)
                    {
                        var src = dst - offset;
                        var x = 0;
                        while (dst + x < pixels.Length && pixels[src + x % offset] == pixels[dst + x]) x++;
                        if (x < count) continue;
                        // if (x != count && stack.Peek().Flag == 0x40) continue;
                        return new CompressBlock(0x40, offset, count);
                    }

                    // 0x80
                    for (var offset = Math.Min(stride * 0x02 + 0x08, dst); offset > stride * 0x02 - 0x08; offset--)
                    {
                        var src = dst - offset;
                        var x = 0;
                        while (dst + x < pixels.Length && pixels[src + x % offset] == pixels[dst + x]) x++;
                        if (x < count) continue;
                        // if (x != count && stack.Peek().Flag == 0x80) continue;
                        return new CompressBlock(0x80, offset, count);
                    }

                    // 0xC0
                    for (var offset = Math.Min(0x20, dst); offset > 0x08; offset--)
                    {
                        var src = dst - offset;
                        var x = 0;
                        while (dst + x < pixels.Length && pixels[src + x % offset] == pixels[dst + x]) x++;
                        if (x < count) continue;
                        // if (x != count && stack.Peek().Flag == 0xC0) continue;
                        return new CompressBlock(0xC0, offset, count);
                    }
                }

                // 0xE0
                var e0 = stack.Count == 0x00 || stack.Peek().Flag != 0xE0 || stack.Peek().Count == 0x20;
                return !e0 || length - 0x02 < 0x02
                    ? new CompressBlock(0xE0, 0x00, 0x00)
                    : new CompressBlock(0xE0, length - 0x02, 0x02);
            }

            byte[] Build()
            {
                var temp = new byte[capacity];
                var p = 0x00;
                foreach (var block in stack)
                {
                    switch (block.Flag)
                    {
                        case 0x00:
                        {
                            var mask = 0x00;
                            mask |= (block.Offset - 0x01) << 0x03;
                            if (block.Count < 0x09)
                            {
                                mask |= block.Count - 0x02;
                                temp[p++] = (byte)mask;
                            }
                            else
                            {
                                mask |= 0x07;
                                temp[p++] = (byte)mask;
                                temp[p++] = (byte)block.Count;
                            }
                        }
                            break;
                        case 0x40:
                        {
                            var mask = 0x40;
                            mask |= (stride + 0x08 - block.Offset) << 0x02;
                            if (block.Count < 0x05)
                            {
                                mask |= block.Count - 0x02;
                                temp[p++] = (byte)mask;
                            }
                            else
                            {
                                mask |= 0x03;
                                temp[p++] = (byte)mask;
                                temp[p++] = (byte)block.Count;
                            }
                        }
                            break;
                        case 0x80:
                        {
                            var mask = 0x80;
                            mask |= (stride * 2 + 0x08 - block.Offset) << 0x02;
                            if (block.Count < 0x05)
                            {
                                mask |= block.Count - 0x02;
                                temp[p++] = (byte)mask;
                            }
                            else
                            {
                                mask |= 0x03;
                                temp[p++] = (byte)mask;
                                temp[p++] = (byte)block.Count;
                            }
                        }
                            break;
                        case 0xC0:
                        {
                            var mask = 0xC0;
                            mask |= block.Offset - 0x01;
                            temp[p++] = (byte)mask;
                            temp[p++] = 0x00;
                            temp[p++] = (byte)block.Count;
                        }
                            break;
                        case 0xE0:
                        {
                            var mask = 0xE0;
                            mask |= block.Count - 0x01;
                            temp[p++] = (byte)mask;
                            for (var x = 0; x < block.Count; x++) temp[p++] = pixels[block.Offset + x];
                        }
                            break;
                        default:
                            throw new FormatException($"block type: {block.Flag}");
                    }
                }

                return temp;
            }
        }

        private readonly struct CompressBlock
        {
            public readonly byte Flag;

            public readonly int Offset;

            public readonly int Count;

            public CompressBlock(byte flag, int offset, int count)
            {
                Flag = flag;
                Offset = offset;
                Count = count;
            }

            public int Size => Flag switch
            {
                0x00 => Count < 0x09 ? 1 : 2,
                0x40 => Count < 0x05 ? 1 : 2,
                0x80 => Count < 0x05 ? 1 : 2,
                0xC0 => 3,
                _ => 1 + Count
            };
        }
    }
}