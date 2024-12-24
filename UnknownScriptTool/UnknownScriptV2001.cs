using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Unknown
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public readonly struct UnknownScriptV2001
    {
        public readonly uint Sort;

        public readonly uint X10;

        public readonly uint X18;

        public readonly byte[][] Commands;

        public UnknownScriptV2001(uint sort, byte[] bytes)
        {
            Sort = sort;

            var commands = new List<byte[]>();
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);
            var header = Encoding.ASCII.GetString(reader.ReadBytes(0x10)).TrimEnd('\0');
            if (header != "MINET") throw new FormatException($"header: {header}");
            X10 = reader.ReadUInt32();
            var x14 = reader.ReadUInt32();
            if (x14 != bytes.Length - 0x20) throw new FormatException($"size: {x14}");
            X18 = reader.ReadUInt32();

            stream.Position = 0x0000_0020;
            while (stream.Position < bytes.Length)
            {
                var position = stream.Position;
                var instruction = reader.ReadUInt32();
                var size = 0x04;

                switch (instruction & 0x0000_00FF)
                {
                    // if
                    case 0x16:
                        size += 0x04;
                        size += 0x04;
                        size += 0x04;
                        break;
                    // 
                    case 0x19:
                        size += 0x04;
                        break;
                    // if
                    case 0x1A:
                        size += 0x04;
                        size += 0x04;
                        size += 0x04;
                        break;
                    // 
                    case 0x1B:
                        break;
                    // setdata
                    case 0x20:
                        size += 0x04;
                        size += 0x04;
                        size += 0x04;
                        break;
                    // strings
                    case 0x22:
                        size += 0x04;
                        size += reader.ReadInt32();
                        break;
                    // 
                    case 0x25:
                        break;
                    // loadscript
                    case 0x26:
                        size += 0x04;
                        size += 0x04;
                        break;
                    // setflag
                    case 0x27:
                        size += 0x04;
                        size += 0x04;
                        break;
                    // strings
                    case 0x29:
                        break;
                    // bustshot
                    case 0x2B:
                    case 0x2C:
                    case 0x2D:
                    case 0x2E:
                        size += 0x04;
                        size += 0x04;
                        size += 0x04;
                        size += 0x04;
                        break;
                    // bg
                    case 0x2F:
                    case 0x30:
                    case 0x31:
                    case 0x32:
                        size += 0x04;
                        size += 0x04;
                        size += 0x04;
                        break;
                    // sound
                    case 0x33:
                    case 0x34:
                    case 0x35:
                    case 0x36:
                        size += 0x04;
                        size += 0x04;
                        break;
                    // se
                    case 0x37:
                    case 0x38:
                    case 0x39:
                        size += 0x04;
                        size += 0x04;
                        break;
                    // 
                    case 0x3C:
                        break;
                    // 
                    case 0x3D:
                        break;
                    // calc
                    case 0x40:
                        size += 0x04;
                        size += 0x04;
                        size += 0x04;
                        size += 0x04;
                        break;
                    // 
                    case 0x44:
                        break;
                    // movescenelistpoint
                    case 0x4B:
                        size += 0x04;
                        size += 0x04;
                        size += reader.ReadInt32() * 0x14;
                        size += 0x04;
                        break;
                    // selectmovescene
                    case 0x4C:
                        size += 0x04;
                        size += 0x04;
                        size += 0x04;
                        break;
                    // alz_waittimes
                    case 0x58:
                    case 0x59:
                        size += 0x04;
                        size += 0x04;
                        break;
                    // soundarrange
                    case 0x5A:
                    case 0x5C:
                        size += 0x04;
                        size += 0x04;
                        size += 0x04;
                        break;
                    default:
                        throw new FormatException($"unknown instruction at {Sort}#{position:X8}: {instruction:X8}");
                }

                stream.Position = position;
                commands.Add(reader.ReadBytes(size));
            }

            Commands = commands.ToArray();
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[0x0000_0020 + Commands.Sum(command => command.Length)];
            using var stream = new MemoryStream(bytes);
            using var writer = new BinaryWriter(stream);

            writer.Write(Encoding.ASCII.GetBytes("MINET"));
            stream.Position = 0x0000_0010;
            writer.Write(X10);
            writer.Write(bytes.Length - 0x20);
            writer.Write(X18);

            stream.Position = 0x0000_0020;
            foreach (var command in Commands) writer.Write(command);

            return bytes;
        }
    }
}