using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using ATool;
using Org.BouncyCastle.Utilities.Zlib;

namespace CatSystem
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public readonly struct SceneScript
    {
        public readonly string Name;

        public readonly string Head;

        public readonly KeyValuePair<uint, uint>[] Labels;

        public readonly KeyValuePair<ushort, byte[]>[] Commands;

        public SceneScript(string name, byte[] bytes)
        {
            Name = name;
            Head = GetHead(bytes);
            bytes = Decompress(bytes, Head);

            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            var x00 = reader.ReadUInt32();
            if (x00 != bytes.Length - 0x10) throw new FormatException($"{Name}:00000000 {x00:X8}");
            var x04 = reader.ReadUInt32();
            if (x04 == 0x0000_0000) throw new FormatException($"{Name}:00000004 {x04:X8}");
            var x08 = reader.ReadUInt32();
            if (x08 != x04 * 0x08) throw new FormatException($"{Name}:00000008 {x08:X8}");
            var x0C = reader.ReadUInt32();
            if ((x0C - x08) % 0x04 != 0x00) throw new FormatException($"{Name}:0000000C {x0C:X8}");

            Labels = new KeyValuePair<uint, uint>[x04];
            stream.Position = 0x0000_0010;
            for (var i = 0x00; i < x04; i++)
            {
                var value = reader.ReadUInt32();
                var key = reader.ReadUInt32();
                Labels[i] = new KeyValuePair<uint, uint>(key, value);
            }

            Commands = new KeyValuePair<ushort, byte[]>[(x0C - x08) / 0x04];
            for (var i = 0x00; i < Commands.Length; i++)
            {
                stream.Position = 0x0000_00010 + x08 + i * 0x04;
                var offset = reader.ReadUInt32();
                stream.Position = 0x0000_00010 + x0C + offset;
                var key = reader.ReadUInt16();
                var value = reader.ReadUntilEnd();
                Commands[i] = new KeyValuePair<ushort, byte[]>(key, value);
            }
        }

        public byte[] ToBytes()
        {
            var source = ToSourceBytes();
            switch (Head)
            {
                case "CatScene":
                {
                    var bytes = new byte[Math.Max(source.Length, 0x0040_0000)];
                    using var stream = new MemoryStream(bytes);
                    using var writer = new BinaryWriter(stream);

                    stream.Position = 0x0000_0010;
                    using (var zlib = new ZOutputStreamLeaveOpen(stream, JZlib.Z_BEST_COMPRESSION))
                    {
                        zlib.Write(source);
                    }

                    var size = (int)stream.Position;
                    stream.Position = 0x0000_0000;
                    writer.Write(Encoding.ASCII.GetBytes(Head));
                    writer.Write(size - 0x10);
                    writer.Write(source.Length);
                    writer.Flush();

                    Array.Resize(ref bytes, size);
                    return bytes;
                }
                case "CSTX":
                {
                    var bytes = new byte[Math.Max(source.Length, 0x0040_0000)];
                    using var stream = new MemoryStream(bytes);
                    using var writer = new BinaryWriter(stream);

                    stream.Position = 0x0000_0010;
                    using (var zlib = new ZOutputStreamLeaveOpen(stream, JZlib.Z_BEST_COMPRESSION))
                    {
                        zlib.Write(source);
                    }

                    var size = (int)stream.Position;
                    writer.Write(Encoding.ASCII.GetBytes(Head));
                    writer.Write(size - 0x10);
                    writer.Write(source.Length);
                    writer.Write(0x0000_0001);
                    writer.Flush();

                    Array.Resize(ref bytes, size);
                    return bytes;
                }
                default:
                    return source;
            }
        }

        internal byte[] ToSourceBytes()
        {
            var x08 = Labels.Length * 0x08;
            var x0C = x08 + Commands.Length * 0x04;
            var bytes = new byte[0x10 + x0C + Commands.Sum(text => 0x02 + text.Value.Length + 0x01)];
            using var stream = new MemoryStream(bytes);
            using var writer = new BinaryWriter(stream);

            writer.Write(bytes.Length);
            writer.Write(Labels.Length);
            writer.Write(x08);
            writer.Write(x0C);

            foreach (var label in Labels)
            {
                writer.Write(label.Value);
                writer.Write(label.Key);
            }

            var offset = 0x0000_0000;
            for (var i = 0x00; i < Commands.Length; i++)
            {
                stream.Position = 0x0000_0010 + x08 + i * 0x04;
                writer.Write(offset);

                stream.Position = 0x0000_0010 + x0C + offset;
                writer.Write(Commands[i].Key);
                writer.Write(Commands[i].Value);
                writer.Write((byte)0x00);

                offset += 0x02 + Commands[i].Value.Length + 0x01;
            }

            return bytes;
        }

        private static string GetHead(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            var head = Encoding.ASCII.GetString(reader.ReadBytes(0x04));
            switch (head)
            {
                case "CatS": // CatScene
                    stream.Position = 0x0000_0000;
                    return Encoding.ASCII.GetString(reader.ReadBytes(0x08));
                case "CSTX":
                    return head;
                default:
                    return "";
            }
        }

        private static byte[] Decompress(byte[] bytes, string head)
        {
            switch (head)
            {
                case "CatScene":
                {
                    using var stream = new MemoryStream(bytes);
                    using var reader = new BinaryReader(stream);
                    stream.Position = 0x0000_0008;

                    var x08 = reader.ReadInt32();
                    if (x08 != bytes.Length - 0x10) throw new FormatException($"{head}:00000008 {x08:X8}");
                    var x0C = reader.ReadInt32();
                    using var zlib = new BinaryReader(new ZInputStreamLeaveOpen(stream));
                    return zlib.ReadBytes(x0C);
                }
                case "CSTX":
                {
                    using var stream = new MemoryStream(bytes);
                    using var reader = new BinaryReader(stream);
                    stream.Position = 0x0000_0004;

                    var x04 = reader.ReadInt32();
                    if (x04 != bytes.Length - 0x10) throw new FormatException($"{head}:00000004 {x04:X8}");
                    var x08 = reader.ReadInt32();
                    var x0C = reader.ReadInt32();
                    if (x0C == 0x0000_0000)
                    {
                        if (x04 != x08) throw new FormatException($"{head}:000000008 {x08:X8}");
                        return reader.ReadBytes(x08);
                    }

                    using var zlib = new BinaryReader(new ZInputStreamLeaveOpen(stream));
                    return zlib.ReadBytes(x0C);
                }
                default:
                    return bytes;
            }
        }
    }
}