using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ATool;
using ImageMagick;
using ImageMagick.Formats;

namespace Will
{
    // ReSharper disable once InconsistentNaming
    public readonly struct WillMBF
    {
        public readonly KeyValuePair<string, byte[]>[] Items;

        private readonly uint _x0C;

        public WillMBF(byte[] bytes, Encoding encoding)
        {
            using var steam = new MemoryStream(bytes);
            using var reader = new BinaryReader(steam, encoding);
            var header = encoding.GetString(reader.ReadBytes(0x04));
            if (header != "MBF0") throw new FormatException($"unsupported header: {header}");
            var count = reader.ReadInt32();
            var offset = reader.ReadInt32();
            _x0C = reader.ReadUInt32();
            var x10 = reader.ReadUInt32(); // file size or 0
            if (x10 != bytes.Length) Debug.WriteLine($"MBF0:10 {x10:X8} != {bytes.Length:X8}");

            Items = new KeyValuePair<string, byte[]>[count];
            var index = 0x0000_0020;
            var data = offset;
            for (var i = 0; i < count; i++)
            {
                steam.Position = index;
                var next = reader.ReadUInt16();
                if (next == 0x0000) continue;
                var name = encoding.GetString(reader.ReadBytes(next - 0x02).TrimEnd());
                index += next;

                steam.Position = data;
                var type = Encoding.ASCII.GetString(reader.ReadBytes(0x02));
                if (type != "BC") throw new FormatException($"unsupported type: {type}");
                var size = reader.ReadInt32();
                steam.Position = data;
                var content = reader.ReadBytes(size);
                Items[i] = new KeyValuePair<string, byte[]>(name, content);
                data += size;
            }
        }

        public MagickImageCollection ToImages(int width, int height)
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
            size += Items.Sum(item => item.Key.Length);
            var result = new byte[size];
            
            using var steam = new MemoryStream(result);
            using var writer = new BinaryWriter(steam, encoding);
            
            writer.Write(encoding.GetBytes("MBF0"));
            writer.Write(Items.Length);
            writer.Write(offset);
            writer.Write(_x0C);
            writer.Write(result.Length);
            
            var data = offset;
            foreach (var item in Items)
            {
                steam.Position = index;
                var name = encoding.GetBytes(item.Key);
                var next = (ushort)(0x03 + name.Length);
                writer.Write(next);
                writer.Write(name);
                index += next;
                
                steam.Position = data;
                writer.Write(item.Value);
                data += item.Value.Length;
            }

            return result;
        }
    }
}