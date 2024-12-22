using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ATool;

namespace BGI
{
    internal static class Program
    {
        public static void Main(params string[] args)
        {
            var mode = "";
            var path = "*.arc";
            switch (args.Length)
            {
                case 1:
                    _encoding = null;
                    switch (args[0])
                    {
                        case "-e":
                        case "-i":
                            mode = args[0];

                            var archives = Directory.GetFiles(".", path);
                            if (archives.Length == 0) throw new FileNotFoundException(path);
                            foreach (var file in archives)
                            {
                                var name = Path.GetFileName(file);
                                if (name.Contains("_")) continue;
                                if (!name.StartsWith("data01") && !name.StartsWith("sys")) continue;
                                Main(mode, file);
                            }

                            return;
                        default:
                            if (File.Exists(args[0]))
                            {
                                mode = "-e";
                                path = args[0];
                                break;
                            }

                            if (Directory.Exists(args[0]))
                            {
                                mode = "-i";
                                path = args[0].TrimEnd('~');
                            }

                            break;
                    }

                    break;
                case 2:
                    _encoding = null;
                    mode = args[0];
                    path = args[1];
                    break;
                case 3:
                    mode = args[0];
                    path = args[1];
                    _encoding = Encoding.GetEncoding(args[2]);
                    break;
            }

            switch (mode)
            {
                case "-e":
                    _encoding ??= Encoding.GetEncoding("SHIFT-JIS");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                {
                    using var reader = new BinaryReader(File.OpenRead(path), Encoding.ASCII, true);
                    var files = reader.ReadBurikoArchive();

                    Directory.CreateDirectory($"{path}~");

                    foreach (var file in files)
                    {
                        var patch = new BurikoProgramPatch(file.Key, file.Value);
                        if (patch.Data.Length == 0) continue;
                        Console.WriteLine($"Export {patch.Name}");
                        using var writer = File.CreateText($"{path}~/{patch.Name}.txt");
                        for (var i = 0; i < patch.Data.Length; i++)
                        {
                            var text = _encoding.GetString(patch.Data[i].Value);
                            writer.WriteLine($">{patch.Data[i].Key:X4}");
                            writer.WriteLine($"◇{i:D4}◇{text.Replace("\n", @"\n")}");
                            writer.WriteLine($"◆{i:D4}◆{text.Replace("\n", @"\n")}");
                            writer.WriteLine();
                        }
                    }
                }
                    break;
                case "-i":
                    _encoding ??= Encoding.GetEncoding("GBK");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                {
                    using var reader = new BinaryReader(File.OpenRead(path), Encoding.ASCII, true);
                    var files = reader.ReadBurikoArchive();

                    Directory.CreateDirectory(_encoding.WebName);

                    foreach (var file in files)
                    {
                        var patch = new BurikoProgramPatch(file.Key, file.Value);
                        if (patch.Data.Length == 0) continue;
                        if (!File.Exists($"{path}~/{patch.Name}.txt")) continue;
                        Console.WriteLine($"Import {patch.Name}");
                        foreach (var line in File.ReadLines($"{path}~/{patch.Name}.txt"))
                        {
                            var match = Regex.Match(line, @"◆(\d+)◆(.+)$");
                            if (!match.Success) continue;

                            var index = int.Parse(match.Groups[1].Value);
                            var text = match.Groups[2].Value.Replace(@"\n", "\n");
                            if (_encoding.CodePage == 936) text = text.ReplaceGbkUnsupported();

                            var offset = patch.Data[index].Key;
                            patch.Data[index] = new KeyValuePair<uint, byte[]>(offset, _encoding.GetBytes(text));
                        }

                        using var stream = File.Create($"{_encoding.WebName}/{patch.Name}");
                        using var writer = new BinaryWriter(stream);
                        writer.Write(file.Value, 0x00, (int)patch.Offset);
                        var position = patch.Offset;

                        if (file.Key.EndsWith("._bp"))
                        {
                            foreach (var item in patch.Data)
                            {
                                stream.Position = position;
                                writer.Write(item.Value);
                                writer.Write((byte)0x00);

                                stream.Position = item.Key + 0x01;
                                writer.Write((ushort)(position - item.Key));

                                position += (uint)(item.Value.Length + 0x01);
                            }

                            var limit = (position + 0x0F) & ~0x0Fu;
                            var empty = new byte[limit - position];
                            stream.Position = position;
                            writer.Write(empty);
                        }
                        else
                        {
                            var command = 0x1C + BitConverter.ToUInt32(file.Value, 0x1C);
                            var dictionary = new Dictionary<string, uint>();

                            foreach (var item in patch.Data)
                            {
                                var hash = BitConverter.ToString(item.Value);
                                if (!dictionary.TryGetValue(hash, out var value))
                                {
                                    stream.Position = position;
                                    writer.Write(item.Value);
                                    writer.Write((byte)0x00);

                                    value = position - command;
                                    dictionary.Add(hash, value);

                                    position += (uint)(item.Value.Length + 0x01);
                                }

                                stream.Position = item.Key + 0x04;
                                writer.Write(value);
                            }
                        }
                    }
                }
                    break;
                default:
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  Export text : BurikoScriptTool -e [*.arc] [encoding]");
                    Console.WriteLine("  Import text : BurikoScriptTool -i [*.arc] [encoding]");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
            }
        }

        private static Encoding _encoding = Encoding.Default;

        private static KeyValuePair<string, byte[]>[] ReadBurikoArchive(this BinaryReader reader)
        {
            var head = Encoding.ASCII.GetString(reader.ReadBytes(0x0C));
            if (head == "BURIKO ARC20") return ReadBurikoArchiveV2(reader);
            if (head != "PackFile    ") throw new FormatException($"unsupported: {head}");
            var count = reader.ReadUInt32();
            var patches = new KeyValuePair<string, byte[]>[count];

            // TODO: ...

            return patches;
        }

        private static KeyValuePair<string, byte[]>[] ReadBurikoArchiveV2(this BinaryReader reader)
        {
            reader.BaseStream.Position = 0x0000_000C;
            var count = reader.ReadUInt32();
            var patches = new KeyValuePair<string, byte[]>[count];

            for (var i = 0x00; i < count; i++)
            {
                reader.BaseStream.Position = 0x0000_0010 + i * 0x80;
                var name = Encoding.GetEncoding(932).GetString(reader.ReadBytes(0x60).TrimEnd());
                var offset = 0x0000_0010 + count * 0x80 + reader.ReadUInt32();
                var size = reader.ReadInt32();
                reader.BaseStream.Position = offset;
                var bytes = reader.ReadBytes(size);
                switch (BitConverter.ToUInt32(bytes, 0))
                {
                    case 0x2043_5344:
                        bytes = DecodeDSC(bytes);
                        break;
                }

                patches[i] = new KeyValuePair<string, byte[]>(name, bytes);
            }

            return patches;
        }

        // ReSharper disable once InconsistentNaming
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static byte[] DecodeDSC(this byte[] source)
        {
            using var stream = new MemoryStream(source);
            using var reader = new BinaryReader(stream);
            var header = Encoding.ASCII.GetString(reader.ReadBytes(0x10).TrimEnd());
            switch (header)
            {
                case "DSC FORMAT 1.00":
                    break;
                default:
                    throw new FormatException($"unsupported: {header}");
            }

            stream.Position = 0x0000_0000;
            var magic = reader.ReadUInt32() << 0x10;
            stream.Position = 0x0000_0010;
            var key = reader.ReadUInt32();
            var output = new byte[reader.ReadUInt32()];
            var decompress = reader.ReadInt32();

            stream.Position = 0x0000_0020;
            var codes = new HuffmanCode[0x0200];
            var nodes = new HuffmanNode[0x03FF];

            var index = 0x0000;
            for (var i = 0x0000; i < 0x0200; i++)
            {
                var depth = reader.ReadByte();
                depth = (byte)(depth - UpdateKey());
                if (depth == 0x00) continue;
                codes[index] = new HuffmanCode { Depth = depth, Code = (ushort)i };
                index++;
            }

            Array.Sort(codes, 0x0000, index);
            BuildHuffmanTree(index);
            HuffmanDecompress(decompress);
            return output;

            // form https://github.com/morkt/GARbro
            byte UpdateKey()
            {
                var a = 0x4E35 * (key & 0xFFFF);
                var b = magic | (key >> 0x0010);
                b *= 0x4E35;
                b += key * 0x015A + (a >> 0x0010);
                b &= 0xFFFF;
                key = (b << 0x0010) + (a & 0xFFFF) + 0x0001;
                return (byte)b;
            }

            // form https://github.com/morkt/GARbro
            void BuildHuffmanTree(int capacity)
            {
                var indices = new int[0x0002, 0x0200];
                var next_node_index = 0x0001;
                var depth_nodes = 0x0001;
                var depth = 0x0000;
                var child_index = 0x0000;

                indices[0x0000, 0x0000] = 0x0000;
                index = 0x0000;
                while (index < capacity)
                {
                    var huffman_nodes_index = child_index;
                    child_index ^= 0x0001;

                    var depth_existed_nodes = 0x0000;
                    while (index < codes.Length && codes[index].Depth == depth)
                    {
                        var node = new HuffmanNode { IsParent = false, Code = codes[index++].Code };
                        nodes[indices[huffman_nodes_index, depth_existed_nodes]] = node;
                        depth_existed_nodes++;
                    }

                    var depth_nodes_to_create = depth_nodes - depth_existed_nodes;
                    for (var i = 0x0000; i < depth_nodes_to_create; i++)
                    {
                        var node = new HuffmanNode { IsParent = true };
                        indices[child_index, i * 0x0002] = node.LeftChildIndex = next_node_index++;
                        indices[child_index, i * 0x0002 + 0x0001] = node.RightChildIndex = next_node_index++;
                        nodes[indices[huffman_nodes_index, depth_existed_nodes + i]] = node;
                    }

                    depth++;
                    depth_nodes = depth_nodes_to_create * 0x0002;
                }
            }

            // form https://github.com/morkt/GARbro
            void HuffmanDecompress(int capacity)
            {
                var dst_ptr = 0x0000_0000;
                var cache = 0x0000_0000u;
                var cached = 0x0000_0000;

                for (var k = 0x0000_0000u; k < capacity; k++)
                {
                    var node_index = 0x0000_0000;
                    do
                    {
                        var value = ReadBits(0x01);
                        node_index = 0x0000_0000 == value
                            ? nodes[node_index].LeftChildIndex
                            : nodes[node_index].RightChildIndex;
                    } while (nodes[node_index].IsParent);

                    var code = nodes[node_index].Code;
                    if (code >= 0x0100)
                    {
                        var offset = ReadBits(0x0C);

                        var count = (code & 0xFF) + 0x02;
                        offset += 0x02;
                        var src = (int)(dst_ptr - offset);
                        var dst = dst_ptr;
                        output.CopyOverlapped(src, dst, count);
                        dst_ptr += count;
                    }
                    else
                    {
                        output[dst_ptr++] = (byte)code;
                    }
                }

                return;

                uint ReadBits(int bits)
                {
                    while (cached < bits)
                    {
                        var b = reader.ReadByte();
                        cache = (cache << 0x08) | b;
                        cached += 0x08;
                    }

                    var mask = (uint)((0x01 << bits) - 0x01);
                    cached -= bits;

                    return (cache >> cached) & mask;
                }
            }
        }
    }
}