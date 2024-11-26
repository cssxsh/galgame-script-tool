using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Unknown
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public readonly struct UnknownScriptV2004
    {
        public readonly string Name;

        public readonly uint Sort;

        public readonly uint Key;

        public readonly byte[][] Commands;

        public UnknownScriptV2004(string name, uint sort, byte[] bytes)
        {
            Name = name;
            Sort = sort;

            var commands = new List<byte[]>();
            using var steam = new MemoryStream(bytes);
            using var reader = new BinaryReader(steam);
            var header = reader.ReadUInt32();
            if (header != 0x2054_5543) throw new FormatException($"header: {header:X8}");
            var x04 = reader.ReadUInt32();
            if (x04 != bytes.Length - 0x1C) throw new FormatException($"0x04: {x04:X8}");
            Key = reader.ReadUInt32();
            var x0C = reader.ReadUInt32();
            if (x0C != 0x0001_0000) throw new FormatException($"0x0C: {x0C:X8}");

            var mask = (byte)(0x75 * Key + 0x42);
            for (var i = 0x1C; i < bytes.Length; i++) bytes[i] ^= mask;

            steam.Position = 0x0000_001C;
            while (steam.Position < bytes.Length)
            {
                var position = steam.Position;
                var instruction = reader.ReadUInt16();
                var size = 0x02;

                switch (instruction)
                {
                    case 0x0001:
                    case 0x0002:
                    case 0x0003:
                    case 0x0004:
                    case 0x0005:
                    case 0x000C:
                        break;
                    case 0x000D:
                        size += 0x04;
                        break;
                    case 0x000E:
                    case 0x000F:
                    case 0x0010:
                    case 0x0011:
                    case 0x0012:
                    case 0x0013:
                        break;
                    case 0x0015:
                    case 0x0016:
                    case 0x0017:
                    case 0x0018:
                    case 0x0019:
                    case 0x001A:
                    case 0x001B:
                    case 0x001C:
                    case 0x001D:
                        size += 0x04;
                        break;
                    case 0x001E:
                    case 0x001F:
                        break;
                    case 0x0021:
                        size += 0x04;
                        break;
                    case 0x0026:
                        size += 0x04;
                        size += 0x04;
                        break;
                    case 0x01F4:
                        size += 0x02;
                        break;
                    case 0x0200:
                    case 0x0201:
                        size += 0x02 + reader.ReadUInt16();
                        break;
                    case 0x023F:
                        size += 0x04;
                        break;
                    case 0x0241:
                        size += 0x02 + reader.ReadUInt16();
                        break;
                    case 0x0242:
                    case 0x024B:
                    case 0x024C:
                    case 0x024D:
                    case 0x024E:
                    case 0x024F:
                    case 0x0252:
                    case 0x0253:
                    case 0x0256:
                    case 0x0257:
                    case 0x0258:
                    case 0x025A:
                    case 0x025B:
                    case 0x025D:
                    case 0x0262:
                    case 0x0263:
                    case 0x0265:
                        size += 0x02;
                        size += 0x01;
                        size += 0x01;
                        size += 0x01;
                        size += 0x01;
                        break;
                    case 0x0268:
                    case 0x0269:
                    case 0x026A:
                        size += 0x02;
                        break;
                    case 0x026D:
                    case 0x026E:
                        size += 0x04;
                        break;
                    case 0x0270:
                    case 0x0271:
                        size += 0x02;
                        size += 0x01;
                        size += 0x01;
                        size += 0x01;
                        size += 0x01;
                        break;
                    case 0x0272:
                    case 0x0273:
                    case 0x0274:
                    case 0x0275:
                        size += 0x02;
                        break;
                    case 0x0276:
                        size += 0x04;
                        break;
                    case 0x0277:
                    case 0x0279:
                        size += 0x02;
                        break;
                    case 0x027A:
                        size += (int)(reader.ReadUInt32() + 0x1A - position);
                        break;
                    case 0x027B:
                        break;
                    default:
                        throw new FormatException($"unknown instruction at {Name}:{position:X8} : {instruction:X4}");
                }

                steam.Position = position;
                commands.Add(reader.ReadBytes(size));
            }

            Commands = commands.ToArray();
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[0x0000_001C + Commands.Sum(command => command.Length)];
            using var steam = new MemoryStream(bytes);
            using var writer = new BinaryWriter(steam);

            writer.Write(Encoding.ASCII.GetBytes("CUT "));
            writer.Write((uint)(bytes.Length - 0x1C));
            writer.Write(Key);
            writer.Write(0x0001_0000);

            steam.Position = 0x0000_001C;
            foreach (var command in Commands) writer.Write(command);

            var mask = (byte)(0x75 * Key + 0x42);
            for (var i = 0x1C; i < bytes.Length; i++) bytes[i] ^= mask;

            return bytes;
        }
    }
}