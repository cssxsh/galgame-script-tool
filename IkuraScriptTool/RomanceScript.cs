using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Ikura
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public readonly struct RomanceScript
    {
        public readonly string Name;

        public readonly int[] Labels;

        public readonly byte[][] Commands;

        public RomanceScript(string name, byte[] bytes)
        {
            Name = name;

            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            var offset = reader.ReadUInt32();
            if (offset > bytes.Length) throw new FormatException("bytes length < header size");
            var x04 = reader.ReadUInt32();
            if (x04 != offset) throw new FormatException($"{Name}:00000004 {x04:X8}");
            var x08 = reader.ReadUInt32();
            if (x08 != 0x1C) throw new FormatException($"{Name}:00000008 {x08:X8}");
            var x0C = reader.ReadUInt32();
            if (x0C != x04 - x08) throw new FormatException($"{Name}:0000000C {x0C:X8}");
            var x10 = reader.ReadUInt32();
            if (x10 != offset) throw new FormatException($"{Name}:00000010 {x10:X8}");
            var x14 = reader.ReadUInt32();
            if (x14 != 0x00) throw new FormatException($"{Name}:00000014 {x14:X8}");
            var x18 = reader.ReadUInt32();
            if (x18 != bytes.Length - offset) throw new FormatException($"{Name}:00000018 {x18:X8}");

            Labels = new int[x0C / 0x04];
            var table = new uint[Labels.Length];
            stream.Position = x08;
            for (var i = 0x00; i < Labels.Length; i++) table[i] = reader.ReadUInt32();

            var commands = new List<byte[]>();
            stream.Position = offset;
            while (stream.Position < bytes.Length)
            {
                var position = stream.Position;

                for (var j = 0x00; j < table.Length; j++)
                {
                    if (table[j] != stream.Position - offset) continue;
                    Labels[j] = commands.Count;
                }

                var instruction = reader.ReadByte();
                var size = (int)reader.ReadByte();
                if (size > 0x7F) size = ((size & 0x7F) << 0x08) | reader.ReadByte();

                switch (instruction)
                {
                    // NONE
                    case 0x00:
                        break;
                    // EXIT
                    case 0x01:
                        break;
                    // LS
                    case 0x02:
                    case 0x03:
                    {
                        var raw = reader.ReadBytes(size > 0x7F ? size - 0x03 : size - 0x02);
                        if (!Debugger.IsAttached) break;
                        var text = Encoding.ASCII.GetString(raw).TrimEnd('\0');
                        Debug.WriteLine($"{Name}:{position:X8}>{instruction:X2} '{text}'");
                    }
                        break;
                    case 0x04:
                    case 0x05:
                    case 0x06:
                    case 0x07:
                    case 0x08:
                    case 0x09:
                    // case 0x0A:
                    case 0x0B:
                    case 0x0C:
                    case 0x0D:
                    case 0x0E:
                    // case 0x0F:
                    // case 0x10:
                        break;
                    // PM
                    case 0x11:
                    {
                        var a = reader.ReadUInt32();
                        var b = reader.ReadUInt32();
                        var c = reader.ReadUInt32();
                        if (c != 0xFF00_0000) throw new FormatException($"{instruction:X2} at {Name}:{position:X8}");
                        var raw = reader.ReadBytes(size > 0x7F ? size - 0x10 : size - 0x0F);
                        var end = reader.ReadByte();
                        if (end != 0x00) throw new FormatException($"{instruction:X2} at {Name}:{position:X8}");
                        if (!Debugger.IsAttached) break;
                        var text = Encoding.GetEncoding(932).GetString(raw).TrimEnd('\0');
                        Debug.WriteLine($"{Name}:{position:X8}>{instruction:X2} {a:X8}, {b:X8}, {c:X8}, '{text}', " +
                                        $"{end:X2}");
                    }
                        break;
                    case 0x12:
                    case 0x13:
                        break;
                    // SELECT
                    case 0x14:
                    {
                        var index = reader.ReadByte();
                        var raw = reader.ReadBytes(size > 0x7F ? size - 0x04 : size - 0x03);
                        if (!Debugger.IsAttached) break;
                        var text = Encoding.GetEncoding(932).GetString(raw).TrimEnd('\0');
                        Debug.WriteLine($"{Name}:{position:X8}>{instruction:X2} {index:X2}, '{text}'");
                    }
                        break;
                    case 0x15:
                    // case 0x16:
                    case 0x17:
                    case 0x18:
                    case 0x19:
                    case 0x1A:
                    // case 0x1B:
                    // case 0x1C:
                        break;
                    // CALC
                    case 0x1D:
                        break;
                    // case 0x1E:
                    case 0x1F:
                        break;
                    // BTL
                    case 0x20:
                    {
                        var index = reader.ReadByte();
                        var a = reader.ReadUInt32();
                        var raw = reader.ReadBytes(size > 0x7F ? size - 0x0C : size - 0x0B);
                        var b = reader.ReadUInt32();
                        if (!Debugger.IsAttached) break;
                        var text = Encoding.ASCII.GetString(raw).TrimEnd('\0');
                        Debug.WriteLine($"{Name}:{position:X8}>{instruction:X2} {index:X2}, {a:X2}, '{text}', {b:X2}");
                    }
                        break;
                    case 0x21:
                    case 0x22:
                    case 0x23:
                    case 0x24:
                    case 0x25:
                    case 0x26:
                    case 0x27:
                        break;
                    // Music
                    case 0x28:
                    {
                        var raw = reader.ReadBytes(size > 0x7F ? size - 0x03 : size - 0x02);
                        if (!Debugger.IsAttached) break;
                        var text = Encoding.ASCII.GetString(raw).TrimEnd('\0');
                        Debug.WriteLine($"{Name}:{position:X8}>{instruction:X2} '{text}'");
                    }
                        break;
                    case 0x29:
                    case 0x2A:
                    case 0x2B:
                        break;
                    // SE
                    case 0x2C:
                    {
                        var raw = reader.ReadBytes(size > 0x7F ? size - 0x03 : size - 0x02);
                        if (!Debugger.IsAttached) break;
                        var text = Encoding.ASCII.GetString(raw).TrimEnd('\0');
                        Debug.WriteLine($"{Name}:{position:X8}>{instruction:X2} '{text}'");
                    }
                        break;
                    case 0x2D:
                    case 0x2E:
                    case 0x2F:
                    // case 0x30:
                        break;
                    // PCM
                    case 0x31:
                    {
                        var raw = reader.ReadBytes(size > 0x7F ? size - 0x03 : size - 0x02);
                        if (!Debugger.IsAttached) break;
                        var text = Encoding.ASCII.GetString(raw).TrimEnd('\0');
                        Debug.WriteLine($"{Name}:{position:X8}>{instruction:X2} '{text}'");
                    }
                        break;
                    case 0x32:
                    // case 0x33:
                    // case 0x34:
                    // case 0x35:
                    case 0x36:
                    case 0x37:
                    case 0x38:
                    case 0x39:
                    case 0x3A:
                    // case 0x3B:
                    // case 0x3C:
                        break;
                    // Video
                    case 0x3D:
                    {
                        var a = reader.ReadUInt32();
                        var b = reader.ReadUInt32();
                        var c = reader.ReadUInt32();
                        var d = reader.ReadUInt32();
                        var raw = reader.ReadBytes(size > 0x7F ? size - 0x13 : size - 0x12);
                        if (!Debugger.IsAttached) break;
                        var text = Encoding.GetEncoding(932).GetString(raw).TrimEnd('\0');
                        Debug.WriteLine($"{Name}:{position:X8}>{instruction:X2} {a:X8}, {b:X8}, {c:X8}, {d:X8}, " +
                                        $"'{text}'");
                    }
                        break;
                    // TITLE
                    case 0x3E:
                    {
                        var raw = reader.ReadBytes(size > 0x7F ? size - 0x03 : size - 0x02);
                        if (!Debugger.IsAttached) break;
                        var text = Encoding.GetEncoding(932).GetString(raw).TrimEnd('\0');
                        Debug.WriteLine($"{Name}:{position:X8}>{instruction:X2} '{text}'");
                    }
                        break;
                    case 0x3F:
                    case 0x40:
                    case 0x41:
                    case 0x42:
                    case 0x43:
                    case 0x44:
                    case 0x45:
                    case 0x46:
                    case 0x47:
                    case 0x48:
                    // case 0x49:
                    case 0x4A:
                    // case 0x4B:
                    case 0x4C:
                    case 0x4D:
                    case 0x4E:
                    case 0x4F:
                    // case 0x50:
                    // case 0x51:
                    case 0x52:
                    case 0x53:
                    case 0x54:
                    case 0x55:
                    case 0x56:
                    case 0x57:
                        break;
                    default:
                        throw new FormatException($"{instruction:X2} at {Name}:{position:X8}");
                }

                stream.Position = position;
                commands.Add(reader.ReadBytes(size));
            }

            Commands = commands.ToArray();
        }

        public byte[] ToBytes()
        {
            var offset = (uint)(0x0000_001C + Labels.Length * 0x04);
            var bytes = new byte[offset + Commands.Sum(command => command.Length)];
            using var stream = new MemoryStream(bytes);
            using var writer = new BinaryWriter(stream);

            writer.Write(offset);
            writer.Write(offset);
            writer.Write(0x0000_001C);
            writer.Write(offset - 0x0000_001C);
            writer.Write(offset);
            writer.Write(0x0000_0000);
            writer.Write(bytes.Length - offset);
            stream.Position = offset;
            for (var i = 0x00; i < Commands.Length; i++)
            {
                var position = (uint)stream.Position;

                for (var j = 0x00; j < Labels.Length; j++)
                {
                    if (Labels[j] != i) continue;
                    stream.Position = 0x0000_001C + j * 0x04;
                    writer.Write(position - offset);
                }

                stream.Position = position;
                writer.Write(Commands[i]);
            }

            return bytes;
        }
    }
}