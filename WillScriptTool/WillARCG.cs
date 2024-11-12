using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ATool;

namespace Will
{
    // ReSharper disable once InconsistentNaming
    public readonly struct WillARCG
    {
        public readonly KeyValuePair<string, byte[]>[] Files;

        public WillARCG(byte[] bytes, Encoding encoding)
        {
            using var steam = new MemoryStream(bytes);
            using var reader = new BinaryReader(steam, encoding);
            var header = encoding.GetString(reader.ReadBytes(0x04));
            if (header != "ARCG") throw new FormatException($"unsupported header: {header}");
            var version = reader.ReadUInt32();
            if (version != 0x0001_0000u) throw new FormatException($"unsupported version: {version:X8}");

            var offsetOfIndex = reader.ReadUInt32();
            if (offsetOfIndex == 0x0000_0000u) throw new FormatException($"index offset: {offsetOfIndex:X8}");
            _ = reader.ReadUInt32(); // index size
            var countOfDirectory = reader.ReadUInt16();
            var files = new List<KeyValuePair<string, byte[]>>(reader.ReadInt32());

            for (var j = 0; j < countOfDirectory; j++)
            {
                steam.Position = offsetOfIndex;
                var size = reader.ReadByte();
                var directory = encoding.GetString(reader.ReadBytes(size - 0x01).TrimEnd());
                offsetOfIndex += size;
                var offsetOfDirectory = reader.ReadUInt32();
                var countOfFile = reader.ReadInt32();
                offsetOfIndex += 0x08;

                for (var i = 0; i < countOfFile; i++)
                {
                    steam.Position = offsetOfDirectory;
                    size = reader.ReadByte();
                    var file = encoding.GetString(reader.ReadBytes(size - 0x01).TrimEnd());
                    offsetOfDirectory += size;
                    var offsetOfFile = reader.ReadUInt32();
                    var sizeOfFile = reader.ReadInt32();
                    offsetOfDirectory += 0x08;

                    steam.Position = offsetOfFile;
                    var content = reader.ReadBytes(sizeOfFile);
                    files.Add(new KeyValuePair<string, byte[]>(Path.Combine(directory, file), content));
                }
            }

            Files = files.ToArray();
        }
    }
}