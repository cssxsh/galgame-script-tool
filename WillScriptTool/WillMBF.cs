using System;
using System.Collections.Generic;
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

        public WillMBF(byte[] bytes, Encoding encoding)
        {
            using var steam = new MemoryStream(bytes);
            using var reader = new BinaryReader(steam, encoding);
            var header = encoding.GetString(reader.ReadBytes(0x04));
            if (header != "MBF0") throw new FormatException($"unsupported header: {header}");
            var count = reader.ReadInt32();
            var offset = reader.ReadInt32();
            _ = reader.ReadInt32();
            _ = reader.ReadUInt32(); // file size or 0

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
            if (images.Count - 1 != Items.Length) throw new FormatException($"count no match: {images.Count - 1}");
            for (var i = 0; i < Items.Length; i++)
            {
                var image = images[i + 1];
                if (image.Label != Items[i].Key) throw new FormatException($"layer no match: {image.Label}");
                var bc = new WillBC(Items[i].Value);
                bc.Merge(image as MagickImage);
                Items[i] = new KeyValuePair<string, byte[]>(Items[i].Key, bc.ToBytes());
            }
        }
    }
}