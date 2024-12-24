using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Org.BouncyCastle.Utilities.Zlib;

namespace CatSystem
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public readonly struct FrameScript
    {
        public readonly string Name;

        public readonly byte[] Content;

        public FrameScript(string name, byte[] bytes)
        {
            Name = name;

            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            var head = Encoding.ASCII.GetString(reader.ReadBytes(0x04));
            if (head != "FES\0")
            {
                Content = bytes;
                return;
            }

            var x04 = reader.ReadInt32();
            if (x04 != bytes.Length - 0x10) throw new FormatException($"{Name}:000000004 0x{x04:X8}");
            var x08 = reader.ReadInt32();
            var x0C = reader.ReadUInt32();
            if (x0C != 0x0000_0000u) throw new FormatException($"{Name}:00000000C 0x{x0C:X8}");

            using var zlib = new BinaryReader(new ZInputStreamLeaveOpen(reader.BaseStream));
            Content = zlib.ReadBytes(x08);
            if (x08 != Content.Length) throw new FormatException($"{Name}:000000008 0x{x08:X8}");
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[Math.Max(Content.Length, 0x0040_0000)];

            using var stream = new MemoryStream(bytes);
            using var writer = new BinaryWriter(stream);

            stream.Position = 0x0000_0010;
            using (var zlib = new ZOutputStreamLeaveOpen(stream, JZlib.Z_BEST_COMPRESSION))
            {
                zlib.Write(Content);
            }

            var size = (int)stream.Position;
            stream.Position = 0x0000_0000;
            writer.Write(Encoding.ASCII.GetBytes("FES\0"));
            writer.Write(size - 0x10);
            writer.Write(Content.Length);
            writer.Write(0x0000_0000u);
            writer.Flush();

            Array.Resize(ref bytes, size);
            return bytes;
        }
    }
}