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
    public class KcScript
    {
        public readonly string Name;
        
        public readonly uint X20;
        
        public readonly uint X24;
        
        public readonly uint X30;

        public readonly byte[] Code;

        public readonly KeyValuePair<uint, byte[]>[] Texts;

        public KcScript(string name, byte[] bytes)
        {
            Name = name;

            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            var head = Encoding.ASCII.GetString(reader.ReadBytes(0x04));
            if (head != "KCS\0") throw new NotSupportedException($"unsupported version: {head}.");
            var x04 = reader.ReadUInt32();
            if (x04 != 0x0000_0002u) throw new FormatException($"{Name}:000000004 0x{x04:X8}");
            var x08 = reader.ReadUInt32();
            if (x08 != 0x0000_0000u) throw new FormatException($"{Name}:000000008 0x{x08:X8}");

            var x0C = reader.ReadUInt32();
            if (x0C != 0x0000_0034u) throw new FormatException($"{Name}:00000000C 0x{x0C:X8}");
            var x10 = reader.ReadInt32();

            var x14 = reader.ReadUInt32();
            if (x14 != x10 + x0C) throw new FormatException($"{Name}:000000014 0x{x14:X8}");
            var x18 = reader.ReadInt32();
            var x1C = reader.ReadInt32();
            if (x18 * 0x08 > x1C) throw new FormatException($"{Name}:000000018 0x{x18:X8}");

            X20 = reader.ReadUInt32();
            X24 = reader.ReadUInt32();
            var x28 = reader.ReadUInt32();
            if (x28 != x0C + x10 + x1C) throw new FormatException($"{Name}:000000028 0x{x28:X8}");
            var x2C = reader.ReadUInt32();
            if (x2C != 0x0000_0000u) throw new FormatException($"{Name}:000000008 0x{x08:X8}");
            X30 = reader.ReadUInt32();

            using var zlib = new BinaryReader(new ZInputStreamLeaveOpen(reader.BaseStream));
            Code = zlib.ReadBytes(x10);
            if (x10 != Code.Length) throw new FormatException($"{Name}:000000010 0x{x10:X8}");

            var texts = zlib.ReadBytes(x1C);
            if (x1C != texts.Length) throw new FormatException($"{Name}:00000001C 0x{x1C:X8}");
            Texts = ReadTexts(texts, x18);
        }

        public byte[] ToBytes()
        {
            var capacity = 0x34 + Code.Length + Texts.Sum(text => 0x08 + text.Value.Length);
            var bytes = new byte[Math.Max(capacity, 0x0040_0000)];

            using var stream = new MemoryStream(bytes);
            using var writer = new BinaryWriter(stream);

            stream.Position = 0x0000_0034;
            using (var zlib = new BinaryWriter(new ZOutputStreamLeaveOpen(stream, JZlib.Z_BEST_COMPRESSION)))
            {
                zlib.Write(Code);
                var offset = Texts.Length * 0x08;
                for (var i = 0x00; i < Texts.Length; i++)
                {
                    zlib.Write(offset);
                    zlib.Write(Texts[i].Key);
                }
                for (var i = 0x00; i < Texts.Length; i++)
                {
                    zlib.Write(Texts[i].Value);
                    zlib.Write((ushort)0x0000);
                    offset += Texts[i].Value.Length + 0x02;
                }
            }
            
            var size = (int)stream.Position;
            stream.Position = 0x0000_0000;
            writer.Write("KCS\0");
            writer.Write(0x0000_0002u);
            writer.Write(0x0000_0000u);
            writer.Write(0x0000_0034u);
            writer.Write(Code.Length);
            writer.Write(0x0000_0034u + (uint)Code.Length);
            writer.Write((uint)Texts.Length);
            writer.Write(Texts.Sum(text => 0x08 + text.Value.Length));
            writer.Write(X20);
            writer.Write(X24);
            writer.Write(capacity);
            writer.Write(0x0000_0000u);
            writer.Write(X30);
            writer.Flush();

            Array.Resize(ref bytes, size);
            return bytes;
        }

        private static KeyValuePair<uint, byte[]>[] ReadTexts(byte[] bytes, int count)
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            var texts = new KeyValuePair<uint, byte[]>[count];
            for (var i = 0x00; i < count; i++)
            {
                stream.Position = i * 0x08;
                var offset = reader.ReadUInt32();
                var key = reader.ReadUInt32();
                stream.Position = offset;
                var value = reader.ReadUntilEnd();
                texts[i] = new KeyValuePair<uint, byte[]>(key, value);
            }

            return texts;
        }
    }
}