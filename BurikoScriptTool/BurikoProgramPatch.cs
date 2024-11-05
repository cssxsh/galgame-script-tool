using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using ATool;

namespace BGI
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public readonly struct BurikoProgramPatch
    {
        public readonly string Name;

        public readonly uint Header;

        public readonly uint Offset;

        public readonly KeyValuePair<uint, byte[]>[] Data;

        public BurikoProgramPatch(string name, byte[] source)
        {
            Name = name;

            using var stream = new MemoryStream(source);
            using var reader = new BinaryReader(stream);

            var data = new List<KeyValuePair<uint, byte[]>>();
            Header = reader.ReadUInt32();
            if (Header == 0x6972_7542u) // 'BurikoCompiledScript'
            {
                stream.Position = 0x0000_0000;
                var header = Encoding.ASCII.GetString(reader.ReadBytes(0x1C).TrimEnd());
                var command = 0x0000_001C + reader.ReadUInt32();
                var size = (uint)source.Length;
                stream.Position = command;
                while (stream.Position < size)
                {
                    var position = (uint)stream.Position;
                    var instruction = reader.ReadUInt32();
                    var capacity = instruction switch
                    {
                        0x01 => 4,
                        0x03 => 4,
                        _ => throw new FormatException($"{position:X8}: {instruction:X2}")
                        0x7F => 4,
                        0xF4 => 0,
                        0xF9 => 0,
                    };

                    if (instruction == 0x03 || instruction == 0x7F)
                    {
                        var offset = reader.ReadUInt32();
                        stream.Position = command + offset;
                        if (size > stream.Position) size = (uint)stream.Position;
                        var count = (int)(source.Length - offset > 0x80 ? 0x80 : source.Length - offset);
                        var bytes = reader.ReadBytes(count).TrimEnd();
                        data.Add(new KeyValuePair<uint, byte[]>(position, bytes));
                    }

                    stream.Position = position + 4 + capacity;
                }

                Offset = size;
            }
            else if (Name.EndsWith("._bp")) // ._bp
            {
                var position = Header;
                var size = reader.ReadUInt32();
                stream.Position = position;
                while (stream.Position < size)
                {
                    position = (uint)stream.Position;
                    var instruction = reader.ReadByte();
                    var capacity = instruction switch
                    {
                        0x00 => 1,
                        0x01 => 2,
                        0x02 => 4,
                        0x04 => 2,
                        0x05 => 2,
                        0x06 => 2,
                        0x08 => 1,
                        0x09 => 1,
                        0x0A => 1,
                        0x0C => 2,
                        0x10 => 0,
                        0x11 => 0,
                        0x14 => 0,
                        0x15 => 1,
                        0x16 => 0,
                        0x17 => 0,
                        0x20 => 0,
                        0x21 => 0,
                        0x22 => 0,
                        0x23 => 0,
                        0x24 => 0,
                        0x25 => 0,
                        0x26 => 0,
                        0x27 => 0,
                        0x28 => 0,
                        0x29 => 0,
                        0x2A => 0,
                        0x2B => 0,
                        0x30 => 0,
                        0x31 => 0,
                        0x32 => 0,
                        0x33 => 0,
                        0x34 => 0,
                        0x35 => 0,
                        0x38 => 0,
                        0x39 => 0,
                        0x3A => 0,
                        0x40 => 0,
                        0x41 => 0,
                        0x42 => 0,
                        0x43 => 0,
                        0x44 => 0,
                        0x45 => 0,
                        0x48 => 0,
                        0x49 => 0,
                        0x50 => 0,
                        0x51 => 0,
                        0x52 => 0,
                        0x53 => 0,
                        0x54 => 0,
                        0x55 => 0,
                        0x56 => 0,
                        0x57 => 0,
                        0x58 => 0,
                        0x59 => 0,
                        0x5A => 0,
                        0x5B => 0,
                        0x5D => 0,
                        0x5E => 0,
                        0x5F => 0,
                        0x60 => 0,
                        0x61 => 0,
                        0x62 => 0,
                        0x63 => 0,
                        0x64 => 0,
                        0x65 => 0,
                        0x66 => 0,
                        0x67 => 0,
                        0x68 => 0,
                        0x69 => 0,
                        0x6A => 0,
                        0x6B => 0,
                        0x6C => 0,
                        0x6D => 0,
                        0x6E => 0,
                        0x6F => 0,
                        0x70 => 0,
                        0x71 => 0,
                        0x74 => 0,
                        0x75 => 0,
                        0x77 => 0,
                        0x78 => 0,
                        0x79 => 0,
                        0x7A => 0,
                        0x7B => 0,
                        0x7C => 0,
                        0x7D => 0,
                        0x7E => 0,
                        0x7F => 0,
                        0x80 => 1,
                        0x81 => 1,
                        0x90 => 1,
                        0x91 => 1,
                        0x92 => 1,
                        0xA0 => 1,
                        0xB0 => 1,
                        0xC0 => 1,
                        0xE0 => 1,
                        0xFF => 1,
                        _ => throw new FormatException($"{Name} at {position:X8}: {instruction:X2}")
                    };

                    if (instruction == 0x05)
                    {
                        var offset = reader.ReadUInt16();
                        stream.Position = position + offset;
                        if (size > stream.Position) size = (uint)stream.Position;
                        var count = source.Length - offset > 0x80 ? 0x80 : source.Length - offset;
                        var bytes = reader.ReadBytes(count).TrimEnd();
                        data.Add(new KeyValuePair<uint, byte[]>(position, bytes));
                    }

                    stream.Position = position + 1 + capacity;
                }

                Offset = size;
            }
            else
            {
                Offset = (uint)source.Length;
            }

            Data = data.ToArray();
        }
    }
}