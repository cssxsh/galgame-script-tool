using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using ATool;

namespace BGI
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public readonly struct BurikoProgramPatch
    {
        public readonly uint Offset;

        public readonly KeyValuePair<uint, byte[]>[] Data;

        public BurikoProgramPatch(byte[] source)
        {
            using var stream = new MemoryStream(source);
            using var reader = new BinaryReader(stream);
            var position = reader.ReadUInt32();
            var size = reader.ReadUInt32();

            var data = new List<KeyValuePair<uint, byte[]>>();
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
                    // 0x33 => Array.Empty<byte>(),
                    // 0x34 => Array.Empty<byte>(),
                    // 0x35 => Array.Empty<byte>(),
                    // 0x38 => Array.Empty<byte>(),
                    // 0x39 => Array.Empty<byte>(),
                    // 0x3A => Array.Empty<byte>(),
                    // 0x40 => Array.Empty<byte>(),
                    // 0x42 => Array.Empty<byte>(),
                    // 0x43 => Array.Empty<byte>(),
                    // 0x48 => Array.Empty<byte>(),
                    // 0x49 => Array.Empty<byte>(),
                    // 0x60 => Array.Empty<byte>(),
                    // 0x61 => Array.Empty<byte>(),
                    // 0x62 => Array.Empty<byte>(),
                    // 0x63 => Array.Empty<byte>(),//
                    // 0x64 => Array.Empty<byte>(),
                    // 0x65 => Array.Empty<byte>(),
                    // 0x67 => Array.Empty<byte>(),
                    // 0x68 => Array.Empty<byte>(),
                    // 0x69 => Array.Empty<byte>(),
                    // 0x6A => Array.Empty<byte>(),
                    // 0x6B => Array.Empty<byte>(),
                    // 0x6C => Array.Empty<byte>(),
                    // 0x6D => Array.Empty<byte>(),
                    // 0x6F => Array.Empty<byte>(),
                    // 0x77 => Array.Empty<byte>(),
                    // 0x78 => Array.Empty<byte>(),
                    // 0x79 => Array.Empty<byte>(),
                    // 0x7A => Array.Empty<byte>(),
                    // 0x7B => Array.Empty<byte>(),
                    0x80 => 1,
                    0x81 => 1,
                    0x90 => 1,
                    0x91 => 1,
                    0x92 => 1,
                    0xA0 => 1,
                    0xB0 => 1,
                    0xC0 => 1,
                    _ => throw new FormatException($"{stream.Position - 1:X8}: {instruction:X2}")
                };

                if (instruction == 0x05)
                {
                    var offset = reader.ReadUInt16();
                    if (size > offset) size = offset;
                    stream.Position = position + offset;
                    var count = source.Length - offset > 0x80 ? 0x80 : source.Length - offset;
                    var bytes = reader.ReadBytes(count).TrimEnd();
                    data.Add(new KeyValuePair<uint, byte[]>(position + 1, bytes));
                }

                stream.Position = position + 1 + capacity;
            }

            Offset = size;
            Data = data.ToArray();
        }
    }
}