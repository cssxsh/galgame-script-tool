using System;
using System.Collections.Generic;
using System.IO;
using ImageMagick;

namespace rUGP
{
    // ReSharper disable MemberCanBeProtected.Global
    // ReSharper disable MemberCanBePrivate.Global
    // ReSharper disable ConvertIfStatementToReturnStatement
    public class CRip : CSbm
    {
        public Rect ValidateRect { protected set; get; }

        public uint Flags;

        protected uint X3C;

        public byte[] Compressed;

        public CRip(string name, byte[] bytes)
        {
            Name = name;

            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            Version = reader.ReadUInt32();
            switch (Version)
            {
                case 0x45:
                {
                    var x = reader.ReadUInt16();
                    var y = reader.ReadUInt16();
                    Width = reader.ReadUInt16();
                    Height = reader.ReadUInt16();
                    var w = reader.ReadUInt16();
                    var h = reader.ReadUInt16();
                    ValidateRect = new Rect(x, y, w, h);
                    Flags = reader.ReadUInt32();
                    var size = reader.ReadInt32();
                    X3C = reader.ReadUInt32();
                    Compressed = reader.ReadBytes(size);
                }
                    break;
                default:
                    throw new NotSupportedException($"version {Version} not supported");
            }
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
                    0x01 => UnCompressRgb1(),
                    0x02 => UnCompressRgb2(),
                    0x03 => UnCompressRgb3(),
                    _ => null
                },
                0x03 => ((Flags >> 0x10) & 0xFF) switch
                {
                    0x02 => UnCompressRgba1(),
                    _ => null
                },
                _ => null
            } ?? throw new FormatException($"unsupported flags: {Flags:X8}");

            return new MagickImage(data, settings);
        }

        public override void Merge(MagickImage image)
        {
            var data = image.ToByteArray((Flags & 0xFF) switch
            {
                0x01 => MagickFormat.Gray,
                0x02 => MagickFormat.Bgra, // BGRx
                0x03 => MagickFormat.Bgra,
                _ => throw new FormatException($"unsupported flags: {Flags:X8}"),
            });

            Compressed = (Flags & 0xFF) switch
            {
                0x01 => CompressToSia(data),
                0x02 => ((Flags >> 0x10) & 0xFF) switch
                {
                    0x01 => throw new NotImplementedException("?CompressDibRgb1@CRip@@QAEHABVCDib@@@Z"),
                    0x02 => throw new NotImplementedException("?CompressDibRgb2@CRip@@QAEHABVCDib@@@Z"),
                    0x03 => throw new NotImplementedException("?CompressDibRgb3@CRip@@QAEHABVCDib@@@Z"),
                    _ => null
                },
                0x03 => ((Flags >> 0x10) & 0xFF) switch
                {
                    0x02 => throw new NotImplementedException(
                        "?CompressDibRgba@CRip@@UAEHV?$CCRef@VCS5i@@VCS5i_ome@@VTS5i@@@@ABUtagSQR@@H@Z"),
                    _ => null
                },
                _ => null
            } ?? throw new FormatException($"unsupported flags: {Flags:X8}");
        }

        // ?UnCompressSia@CRip@@QBEXV?$CRef@VCImgArea@@VCImgArea_ome@@VTImgArea@@@@@Z
        private byte[] UnCompressSia()
        {
            var output = new byte[Width * Height * 1];
            var src = 0;
            var stride = Width * 1;

            for (var y = 0; y < ValidateRect.H; y++)
            {
                var dst = (ValidateRect.Y + y) * stride;
                var gray = (byte)0;
                for (var x = 0;
                     x < ValidateRect.W;
                     gray = (x < ValidateRect.W && src < Compressed.Length) ? Compressed[src++] : gray)
                {
                    var count = Compressed[src++];
                    for (var i = 0; i < count; i++) output[dst++] = gray;
                    x += count;
                }
            }

            return output;
        }

        // ?UnCompressRgb1@CRip@@QBEXPAUIS5i@@@Z
        private byte[] UnCompressRgb1()
        {
            var output = new byte[Width * Height * 4];
            var stride = Width * 4;
            var bgr = 0u;
            var position = 0x00;

            for (var y = 0; y < Height; y++)
            {
                var dst = (Height - y - 1) * stride;
                for (var x = 0; x < Width; x++)
                {
                    if (ReadBit(ref position))
                    {
                        var b = ReadFC02(ref position, bgr >> 0x00);
                        var g = ReadFC02(ref position, bgr >> 0x08);
                        var r = ReadFC02(ref position, bgr >> 0x10);
                        bgr = b << 0x00 | g << 0x08 | r << 0x10;
                    }

                    BitConverter.GetBytes(bgr | 0xFF000000u).CopyTo(output, dst);
                    dst += 4;
                }
            }

            return output;
        }

        // ?UnCompressRgb2@CRip@@QBEXPAUIS5i@@@Z
        private byte[] UnCompressRgb2()
        {
            var output = new byte[Width * Height * 4];
            var stride = Width * 4;
            var bgr = 0u;
            var position = 0x00;

            for (var y = 0; y < Height; y++)
            {
                var dst = (Height - y - 1) * stride;
                for (var x = 0; x < Width; x++)
                {
                    if (BitConverter.ToUInt32(output, dst) == 0xFE000000u)
                    {
                        if (ReadBit(ref position))
                        {
                            var b = ReadFC02(ref position, bgr >> 0x00);
                            var g = ReadFC02(ref position, bgr >> 0x08);
                            var r = ReadFC02(ref position, bgr >> 0x10);
                            bgr = r << 0x10 | g << 0x08 | b << 0x00;
                        }

                        BitConverter.GetBytes(bgr | 0xFF000000u).CopyTo(output, dst);
                    }
                    else
                    {
                        bgr = BitConverter.ToUInt32(output, dst);
                    }

                    if (dst < stride) continue;
                    BitConverter.GetBytes(ReadBit(ref position) ? bgr | 0xFF000000u : 0xFE000000u)
                        .CopyTo(output, dst - stride);

                    dst += 4;
                }
            }

            return output;
        }

        // ?UnCompressRgb3@CRip@@QBEXPAUIS5i@@@Z
        private byte[] UnCompressRgb3()
        {
            var output = new byte[Width * Height * 4];
            var stride = Width * 4;
            var bgr = 0u;
            var position = 0x00;

            for (var y = 0; y < Height; y++)
            {
                var dst = (Height - y - 1) * stride;
                for (var x = 0; x < Width; x++)
                {
                    if (ReadBit(ref position))
                    {
                        var b = ReadFC02(ref position, bgr >> 0x00) + 0x03;
                        var g = ReadFE(ref position, bgr >> 0x08) + 0x01;
                        var r = ReadFC02(ref position, bgr >> 0x10) + 0x03;
                        bgr = b << 0x00 | g << 0x08 | r << 0x10;
                    }

                    BitConverter.GetBytes(bgr | 0xFF000000u).CopyTo(output, dst);
                    dst += 4;
                }
            }

            return output;
        }

        // ?UnCompressRgba1@CRip@@QBEXPAUIS5i@@PBUtagRBDY@@1W4tagRipRop@1@@Z
        private byte[] UnCompressRgba1()
        {
            var output = new byte[ValidateRect.W * ValidateRect.H * 4];
            var src = BitConverter.ToUInt32(Compressed, 0x00);
            var position = 0x20;

            var dst = 0;
            for (var y = 0; y < ValidateRect.H; y++)
            {
                var rgb = 0u;
                var alpha = (byte)0;
                for (var x = 0; x < ValidateRect.W;)
                {
                    int len = Compressed[src++];
                    if (alpha != 0)
                    {
                        for (var i = 0; i < len; i++)
                        {
                            if (ReadBit(ref position))
                            {
                                var b = ReadFC(ref position, rgb >> 0x00) + 0x03;
                                var g = ReadFC(ref position, rgb >> 0x08) + 0x03;
                                var r = ReadFC(ref position, rgb >> 0x10) + 0x03;
                                rgb = r << 0x10 | g << 0x08 | b << 0x00;
                            }

                            BitConverter.GetBytes(rgb | (uint)(alpha << 0x18)).CopyTo(output, dst);
                            dst += 4;
                        }
                    }
                    else
                    {
                        dst += len * 4;
                    }

                    x += len;
                    if (x >= ValidateRect.W) break;
                    alpha = ReadBit(ref position)
                        ? (byte)(0x01 + (ReadBits(ref position, 0x07) << 0x01))
                        : (byte)(ReadBit(ref position) ? 0xFFu : 0x00u);
                }
            }

            return output;
        }

        // ?CompressDibToSia@CRip@@QAEHABVCDib@@ABUtagSQR@@@Z
        private byte[] CompressToSia(byte[] input)
        {
            var compressed = new List<byte>(input.Length);
            var stride = Width * 1;

            for (var y = 0; y < ValidateRect.H; y++)
            {
                var dst = (ValidateRect.Y + y) * stride;
                var gray = (byte)0;
                var count = 0;
                for (var x = 0; x < ValidateRect.W; x++, dst++)
                {
                    if (input[dst] == gray)
                    {
                        count++;
                    }
                    else
                    {
                        compressed.Add((byte)count);
                        gray = input[dst];
                        compressed.Add(gray);
                        count = 1;
                    }

                    if (count != 0xFF) continue;
                    compressed.Add((byte)count);
                    compressed.Add(gray);
                    count = 0;
                }

                if (count != 0x00)
                {
                    compressed.Add((byte)count);
                }
            }

            return compressed.ToArray();
        }

        protected CRip(string name)
        {
            Name = name;
        }

        protected bool ReadBit(ref int position)
        {
            return (Compressed[position >> 0x03] & (0x80 >> (position++ & 0x07))) != 0;
        }

        protected uint ReadBits(ref int position, int count)
        {
            var x = 0x00u;
            for (var i = 0; i < count; i++) x = (x << 0x01) | (ReadBit(ref position) ? 0x01u : 0x00u);
            return x;
        }

        protected void WriteBit(List<byte> buffer, ref int position, bool value)
        {
            while ((position >> 0x03) >= buffer.Count) buffer.Add(0);
            if (value)
            {
                buffer[position >> 0x03] |= (byte)(0x80 >> (position & 0x07));
            }
            else
            {
                buffer[position >> 0x03] &= (byte)~(0x80 >> (position & 0x07));
            }
            position++;
        }

        protected void WriteBits(List<byte> buffer, ref int position, uint value, int count)
        {
            while (count != 0)
            {
                WriteBit(buffer, ref position, (value >> 0x03 >> count) == 0x01);
                count++;
            }
        }

        // ReSharper disable once InconsistentNaming
        private uint ReadFC02(ref int position, uint prev)
        {
            var flag = prev & 0xFC;
            switch (flag >> 0x02)
            {
                case 0:
                    if (!ReadBit(ref position)) return flag;
                    if (!ReadBit(ref position)) return ReadBits(ref position, 0x06) << 0x02;
                    if (!ReadBit(ref position)) return 0x04;
                    if (!ReadBit(ref position)) return 0x08;
                    return 0x0C;
                case 1:
                    if (!ReadBit(ref position)) return ReadBits(ref position, 0x01) << 0x02;
                    if (!ReadBit(ref position)) return ReadBits(ref position, 0x06) << 0x02;
                    if (!ReadBit(ref position)) return 0x08;
                    if (!ReadBit(ref position)) return 0x0C;
                    return 0x10;
                case 0x3F:
                    if (!ReadBit(ref position)) return 0xFC;
                    if (!ReadBit(ref position)) return ReadBits(ref position, 0x06) << 0x02;
                    if (!ReadBit(ref position)) return 0xF8;
                    return 0xF4 + (uint)(-(int)ReadBits(ref position, 0x01) & 0xFC);
            }

            return ReadFC(ref position, prev);
        }

        // ReSharper disable once InconsistentNaming
        private uint ReadFE(ref int position, uint prev)
        {
            var flag = prev & 0xFE;
            if (!ReadBit(ref position)) return ReadBit(ref position) ? ReadBits(ref position, 0x06) << 0x02 : flag;
            if (!ReadBit(ref position)) return ReadBit(ref position) ? flag - 0x02 : flag + 0x02;
            if (!ReadBit(ref position)) return ReadBit(ref position) ? flag - 0x04 : flag + 0x04;
            return ReadBits(ref position, 0x02) switch
            {
                0x00 => Math.Min(flag + 0x08, 0xFE),
                0x01 => Math.Max(flag - 0x08, 0x00),
                0x02 => Math.Min(flag + 0x0C, 0xFE),
                _ => Math.Max(flag + 0x0C, 0x00)
            };
        }

        // ReSharper disable once InconsistentNaming
        private uint ReadFC(ref int position, uint prev)
        {
            var flag = prev & 0xFC;
            if (!ReadBit(ref position)) return ReadBit(ref position) ? ReadBits(ref position, 0x06) << 0x02 : flag;
            if (!ReadBit(ref position)) return ReadBit(ref position) ? flag - 0x04 : flag + 0x04;
            if (!ReadBit(ref position)) return ReadBit(ref position) ? flag - 0x08 : flag + 0x08;
            return ReadBits(ref position, 0x02) switch
            {
                0x00 => Math.Min(flag + 0x10, 0xFC),
                0x01 => Math.Max(flag - 0x10, 0x00),
                0x02 => Math.Min(flag + 0x18, 0xFC),
                _ => Math.Max(flag + 0x18, 0x00)
            };
        }
    }
}