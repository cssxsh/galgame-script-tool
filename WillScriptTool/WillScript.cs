using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Will
{
    public readonly struct WillScript
    {
        public readonly string Name;

        public readonly byte[][] Commands;

        public WillScript(string name, byte[] bytes)
        {
            Name = name;

            var commands = new List<byte[]>();
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);
            while (stream.Position < bytes.Length)
            {
                var position = stream.Position;
                var size = reader.ReadByte();
                if (size == 0x00 || size == 0xFF)
                {
                    stream.Position = position;
                    var temp = reader.ReadBytes((int)(bytes.Length - position));
                    if (temp.Any(b => b != size)) throw new FormatException($"{name} at 0x{position:X8}");
                    commands.Add(temp);
                    break;
                }

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

            return bytes;
        }
    }
}