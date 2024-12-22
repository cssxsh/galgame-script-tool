using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ATool;

namespace SystemEpsylon
{
    public readonly struct SystemEpsylonScript
    {
        public readonly string Name;

        public readonly uint Flags;

        public readonly byte[][] Commands;

        public SystemEpsylonScript(string name, uint flags, byte[] bytes)
        {
            Name = name;
            Flags = flags;

            if (0x0000_0000 != (Flags & 0x0001_0000))
            {
                var key = (uint)bytes.Length >> 0x02;
                key ^= key << (int)((key & 0x07) + 0x08);
                for (var i = 0x00; i < bytes.Length; i += 0x04)
                {
                    if (bytes.Length - i < 0x04) break;
                    var temp = BitConverter.ToUInt32(bytes, i) ^ key;
                    BitConverter.GetBytes(temp).CopyTo(bytes, i);
                    var move = (int)(temp % 0x18) & 0x1F;
                    key = key << move | key >> (0x20 - move);
                }
            }

            bytes.Xor(0xFF);

            var commands = new List<byte[]>();
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);
            while (stream.Position < bytes.Length)
            {
                var position = stream.Position;
                var instruction = reader.ReadByte();
                var size = reader.ReadByte();

                switch (instruction)
                {
                    case 0x00:
                    case 0x02:
                    case 0x0D:
                    case 0x18:
                    {
                        _ = reader.ReadByte();
                        var len = reader.ReadByte();
                        size += len;

                        // _ = reader.ReadBytes(len);
                    }
                        break;
                    case 0x1E:
                    case 0x1F:
                    case 0x20:
                    {
                        _ = reader.ReadByte();
                        _ = reader.ReadByte();
                        _ = reader.ReadByte();
                        _ = reader.ReadByte();
                        _ = reader.ReadByte();
                        var len = reader.ReadByte();
                        size += len;

                        // _ = reader.ReadBytes(len);
                    }
                        break;
                    case 0x26:
                    {
                        _ = reader.ReadByte();
                        var len = reader.ReadByte();
                        size += len;

                        // _ = reader.ReadBytes(len);
                    }
                        break;
                    case 0x36:
                    {
                        _ = reader.ReadByte();
                        var len = reader.ReadByte();
                        size += len;

                        // _ = reader.ReadByte();
                        // _ = reader.ReadByte();
                        // _ = reader.ReadByte();
                        // _ = reader.ReadByte();

                        // _ = reader.ReadBytes(len);
                    }
                        break;
                    case 0x3A:
                    {
                        var len = reader.ReadByte();
                        size += len;
                        // _ = reader.ReadByte();

                        // _ = reader.ReadByte();
                        // _ = reader.ReadByte();
                        // _ = reader.ReadByte();
                        // _ = reader.ReadByte();

                        // _ = reader.ReadByte();
                        // _ = reader.ReadByte();
                        // _ = reader.ReadByte();
                        // _ = reader.ReadByte();

                        // _ = reader.ReadBytes(len);
                    }
                        break;
                }

                if (size == 0x00) throw new FormatException($"{instruction:X2}");

                stream.Position = position;
                commands.Add(reader.ReadBytes(size));
            }

            Commands = commands.ToArray();
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[Commands.Sum(command => command.Length)];
            var index = 0x00;
            foreach (var command in Commands)
            {
                command.CopyTo(bytes, index);
                index += command.Length;
            }

            bytes.Xor(0xFF);

            // ReSharper disable once InvertIf
            if (0x0000_0000 != (Flags & 0x0001_0000))
            {
                var key = (uint)bytes.Length >> 0x02;
                key ^= key << (int)((key & 0x07) + 0x08);
                for (var i = 0x00; i < bytes.Length; i += 0x04)
                {
                    if (bytes.Length - i < 0x04) break;
                    var temp = BitConverter.ToUInt32(bytes, i);
                    BitConverter.GetBytes(temp ^ key).CopyTo(bytes, i);
                    var move = (int)(temp % 0x18) & 0x1F;
                    key = key << move | key >> (0x20 - move);
                }
            }

            return bytes;
        }
    }
}