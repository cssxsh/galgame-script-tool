using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Will
{
    // ReSharper disable once InconsistentNaming
    public readonly struct WillARCG
    {
        public readonly KeyValuePair<string, byte[]>[] Files;

        public WillARCG(byte[] bytes, Encoding encoding)
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream, encoding);
            var header = encoding.GetString(reader.ReadBytes(0x04));
            if (header != "ARCG") throw new FormatException($"unsupported header: {header}");
            var version = reader.ReadUInt32();
            if (version != 0x0001_0000u) throw new FormatException($"unsupported version: {version:X8}");

            var offsetOfIndex = reader.ReadUInt32();
            if (offsetOfIndex == 0x0000_0000u) throw new FormatException($"index offset: {offsetOfIndex:X8}");
            _ = reader.ReadUInt32(); // index size
            var countOfDirectory = reader.ReadUInt16();
            var files = new List<KeyValuePair<string, byte[]>>(reader.ReadInt32());

            for (var j = 0x00; j < countOfDirectory; j++)
            {
                stream.Position = offsetOfIndex;
                var size = reader.ReadByte();
                var directory = encoding.GetString(reader.ReadBytes(size - 0x01)).TrimEnd('\0');
                var offsetOfDirectory = reader.ReadUInt32();
                var countOfFile = reader.ReadInt32();
                offsetOfIndex += size + 0x08u;

                for (var i = 0x00; i < countOfFile; i++)
                {
                    stream.Position = offsetOfDirectory;
                    size = reader.ReadByte();
                    var file = encoding.GetString(reader.ReadBytes(size - 0x01)).TrimEnd('\0');
                    var offsetOfFile = reader.ReadUInt32();
                    var sizeOfFile = reader.ReadInt32();
                    offsetOfDirectory += size + 0x08u;

                    stream.Position = offsetOfFile;
                    var content = reader.ReadBytes(sizeOfFile);
                    files.Add(new KeyValuePair<string, byte[]>(Path.Combine(directory, file), content));
                }
            }

            Files = files.ToArray();
        }

        public byte[] ToBytes(Encoding encoding)
        {
            var size = 0x0000_0020;
            var offsetOfFile = size;
            size += Files.Sum(file => file.Value.Length);
            var offsetOfIndex = size;
            var freq = new Dictionary<string, int> { [""] = 0x00 };
            size += 0x10;
            foreach (var file in Files)
            {
                var directory = Path.GetDirectoryName(file.Key) ?? "";
                if (freq.TryGetValue(directory, out var count))
                {
                    freq[directory] = count + 0x01;
                }
                else
                {
                    size += (encoding.GetByteCount(directory) + 0x05) & ~0x03;
                    size += 0x08;
                    freq[directory] = 0x01;
                }
            }

            var offsetOfDirectory = size;
            size += freq.Count * 0x04;
            foreach (var file in Files)
            {
                var filename = Path.GetFileName(file.Key);
                size += (encoding.GetByteCount(filename) + 0x05) & ~0x03;
                size += 0x08;
            }

            var result = new byte[size];

            using var stream = new MemoryStream(result);
            using var writer = new BinaryWriter(stream, encoding);

            writer.Write(encoding.GetBytes("ARCG"));
            writer.Write(0x0001_0000u);
            writer.Write(offsetOfIndex);
            writer.Write((uint)(size - offsetOfIndex));
            writer.Write((ushort)freq.Count);
            writer.Write(Files.Length);

            var index = 0;
            foreach (var item in freq)
            {
                stream.Position = offsetOfIndex;
                var directory = encoding.GetBytes(item.Key);
                var capacity = (directory.Length + 0x05) & ~0x03;
                writer.Write((byte)capacity);
                writer.Write(directory);
                stream.Position = offsetOfIndex + capacity;
                writer.Write(offsetOfDirectory);
                writer.Write(item.Value);
                offsetOfIndex += capacity + 0x08;

                for (var i = 0x00; i < item.Value; i++)
                {
                    var file = Files[index++];
                    stream.Position = offsetOfDirectory;
                    var filename = encoding.GetBytes(Path.GetFileName(file.Key) ?? "");
                    capacity = (filename.Length + 0x05) & ~0x03;
                    writer.Write((byte)capacity);
                    writer.Write(filename);
                    stream.Position = offsetOfDirectory + capacity;
                    writer.Write(offsetOfFile);
                    writer.Write(file.Value.Length);
                    offsetOfDirectory += capacity + 0x08;

                    stream.Position = offsetOfFile;
                    writer.Write(file.Value);
                    offsetOfFile += file.Value.Length;
                }

                offsetOfDirectory += 0x04;
            }

            return result;
        }
    }
}