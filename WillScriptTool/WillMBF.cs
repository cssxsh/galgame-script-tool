﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using ImageMagick;
using ImageMagick.Formats;

namespace Will
{
    // ReSharper disable once InconsistentNaming
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public readonly struct WillMBF
    {
        public readonly KeyValuePair<string, byte[]>[] Items;

        public readonly uint X0C;

        public WillMBF(byte[] bytes, Encoding encoding)
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream, encoding);
            var header = encoding.GetString(reader.ReadBytes(0x04));
            if (header != "MBF0") throw new FormatException($"unsupported header: {header}");
            var count = reader.ReadInt32();
            var offset = reader.ReadInt32();
            X0C = reader.ReadUInt32();
            var x10 = reader.ReadUInt32(); // file size or 0
            if (x10 != bytes.Length) Debug.WriteLine($"MBF0:00000010h {x10:X8} != {bytes.Length:X8}");

            Items = new KeyValuePair<string, byte[]>[count];
            var index = 0x0000_0020;
            var data = offset;
            for (var i = 0; i < count; i++)
            {
                stream.Position = index;
                var next = reader.ReadUInt16();
                if (next == 0x0000) continue;
                var name = encoding.GetString(reader.ReadBytes(next - 0x02)).TrimEnd('\0');
                index += next;

                stream.Position = data;
                var type = Encoding.ASCII.GetString(reader.ReadBytes(0x02));
                if (type != "BC") throw new FormatException($"unsupported type: {type}");
                var size = reader.ReadInt32();
                stream.Position = data;
                var content = reader.ReadBytes(size);
                Items[i] = new KeyValuePair<string, byte[]>(name, content);
                data += size;
            }
        }

        public MagickImageCollection ToImages(uint width = 800, uint height = 600)
        {
            var collection = new MagickImageCollection();
            collection.Add(new MagickImage(new MagickColor("#66CCFF"), width, height));
            collection.First().Quality = 100;
            collection.First().Format = MagickFormat.Psd;
            collection.First().ColorType = ColorType.TrueColorAlpha;
            collection.First().Depth = 8;
            collection.First().Settings
                .SetDefines(new PsdWriteDefines { AdditionalInfo = PsdAdditionalInfoPart.All });

            foreach (var item in Items)
            {
                var bc = new WillBC(item.Value);
                var image = bc.ToImage();
                image.Label = item.Key;
                collection.Add(image);
            }

            return collection;
        }

        public void Merge(MagickImageCollection images)
        {
            var dictionary = new Dictionary<string, MagickImage>();
            for (var i = 1; i < images.Count; i++)
            {
                dictionary.Add(images[i].Label ?? $"L{i}", images[i] as MagickImage);
            }

            for (var i = 0; i < Items.Length; i++)
            {
                var bc = new WillBC(Items[i].Value);
                if (!dictionary.TryGetValue(Items[i].Key, out var image)) continue;
                bc.Merge(image);
                Items[i] = new KeyValuePair<string, byte[]>(Items[i].Key, bc.ToBytes());
            }
        }

        public byte[] ToBytes(Encoding encoding)
        {
            var size = 0x0000_0020;
            var index = size;
            size += Items.Sum(item => 0x03 + encoding.GetByteCount(item.Key));
            size = (size + 0x04 + 0x0F) & ~0x0F;
            var offset = size;
            size += Items.Sum(item => item.Value.Length);
            var result = new byte[size];

            using var stream = new MemoryStream(result);
            using var writer = new BinaryWriter(stream, encoding);

            writer.Write(encoding.GetBytes("MBF0"));
            writer.Write(Items.Length);
            writer.Write(offset);
            writer.Write(X0C);
            writer.Write(result.Length);

            var data = offset;
            foreach (var item in Items)
            {
                stream.Position = index;
                var name = encoding.GetBytes(item.Key);
                var next = (ushort)(0x03 + name.Length);
                writer.Write(next);
                writer.Write(name);
                index += next;

                stream.Position = data;
                writer.Write(item.Value);
                data += item.Value.Length;
            }

            return result;
        }
    }
}