using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATool;

namespace Unknown
{
    internal static class Program
    {
        public static void Main(params string[] args)
        {
            var mode = "";
            var path = "scenario.aqa|Gm05.dat";
            switch (args.Length)
            {
                case 1:
                    _encoding = null;
                    switch (args[0])
                    {
                        case "-e":
                        case "-i":
                            mode = args[0];
                            var packages = Directory
                                .EnumerateFiles("./", "*.*")
                                .Where(file => file.EndsWith("scenario.aqa") || file.EndsWith("Gm05.dat"))
                                .ToArray();
                            if (packages.Length == 0) throw new FileNotFoundException(path);
                            foreach (var file in packages)
                            {
                                if (file.Contains("_")) continue;
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
                    Directory.CreateDirectory($"{path}~");
                    switch (Path.GetExtension(path).ToUpperInvariant())
                    {
                        case ".AQA":
                        {
                            using var reader = new BinaryReader(File.OpenRead(path));
                            var scripts = reader.ReadUnknownScriptV2004();

                            foreach (var script in scripts)
                            {
                                if (!script.HasText()) continue;
                                Console.WriteLine($"Export {script.Name}");
                                using var writer = File.CreateText($"{path}~/{script.Name}.txt");
                                for (var i = 0; i < script.Commands.Length; i++)
                                {
                                    var instruction = BitConverter.ToUInt16(script.Commands[i], 0x00);
                                    foreach (var line in ExportV2004(script.Commands[i]))
                                    {
                                        writer.WriteLine($">{instruction:X4}");
                                        writer.WriteLine($"◇{i:D4}◇{line}");
                                        writer.WriteLine($"◆{i:D4}◆{line}");
                                        writer.WriteLine();
                                    }
                                }
                            }
                        }
                            break;
                        case ".DAT":
                        {
                            using var reader = new BinaryReader(File.OpenRead(path));
                            var scripts = reader.ReadUnknownScriptV2001();

                            foreach (var script in scripts)
                            {
                                if (!script.HasText()) continue;
                                Console.WriteLine($"Export {script.Sort:D8}");
                                using var writer = File.CreateText($"{path}~/{script.Sort:D8}.txt");
                                for (var i = 0; i < script.Commands.Length; i++)
                                {
                                    var instruction = BitConverter.ToUInt32(script.Commands[i], 0x00);
                                    foreach (var line in ExportV2001(script.Commands[i]))
                                    {
                                        writer.WriteLine($">{instruction:X8}");
                                        writer.WriteLine($"◇{i:D4}◇{line}");
                                        writer.WriteLine($"◆{i:D4}◆{line}");
                                        writer.WriteLine();
                                    }
                                }
                            }
                        }
                            break;
                        default:
                            throw new FormatException($"unsupported format: {path}");
                    }

                    break;
                case "-i":
                    _encoding ??= Encoding.GetEncoding("GBK");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                    switch (Path.GetExtension(path).ToUpperInvariant())
                    {
                        case ".AQA":
                        {
                            using var reader = new BinaryReader(File.OpenRead(path));
                            var scripts = reader.ReadUnknownScriptV2004();
                            reader.BaseStream.Position = 0x0000_0008;
                            var key = reader.ReadUInt32();

                            foreach (var script in scripts)
                            {
                                if (!script.HasText()) continue;
                                if (!File.Exists($"{path}~/{script.Name}.txt")) continue;
                                Console.WriteLine($"Import {script.Name}");
                                var translated = new string[script.Commands.Length][];
                                foreach (var line in File.ReadLines($"{path}~/{script.Name}.txt"))
                                {
                                    var m = Regex.Match(line, @"◆(\d+)◆(.+$)");
                                    if (!m.Success) continue;

                                    var index = int.Parse(m.Groups[1].Value);
                                    var text = m.Groups[2].Value;

                                    var lines = translated[index] ?? Array.Empty<string>();
                                    Array.Resize(ref lines, lines.Length + 1);
                                    lines[lines.Length - 1] = text;
                                    translated[index] = lines;
                                }

                                // ReSharper disable once InconsistentNaming
                                var _O2I = new Dictionary<int, int>();
                                var offset = 0x0000_0000;
                                for (var i = 0x00; i < script.Commands.Length; i++)
                                {
                                    _O2I.Add(offset, i);
                                    offset += script.Commands[i].Length;
                                }

                                // ReSharper disable once InconsistentNaming
                                var _I2O = new Dictionary<int, int>();
                                offset = 0x0000_0000;
                                for (var i = 0x00; i < script.Commands.Length; i++)
                                {
                                    _I2O.Add(i, offset);
                                    if (translated[i] != null)
                                    {
                                        script.Commands[i] = ImportV2004(script.Commands[i], translated[i]);
                                    }

                                    offset += script.Commands[i].Length;
                                }

                                foreach (var command in script.Commands)
                                {
                                    if (command[0x00] != 0x0D && command[0x00] != 0x21) continue;
                                    var o = BitConverter.ToInt32(command, 0x02);
                                    o = _I2O[_O2I[o]];
                                    BitConverter.GetBytes(o).CopyTo(command, 0x02);
                                }
                            }

                            var filename = path.PatchFileName(_encoding.WebName);
                            Console.WriteLine($"Write {filename}");
                            using var writer = new BinaryWriter(File.Create(filename));
                            writer.WriteUnknownScriptV2004(scripts, key);
                        }
                            break;
                        case ".DAT":
                        {
                            using var reader = new BinaryReader(File.OpenRead(path));
                            var scripts = reader.ReadUnknownScriptV2001();

                            foreach (var script in scripts)
                            {
                                if (!script.HasText()) continue;
                                if (!File.Exists($"{path}~/{script.Sort:D8}.txt")) continue;
                                Console.WriteLine($"Import {script.Sort:D8}");
                                var translated = new string[script.Commands.Length][];
                                foreach (var line in File.ReadLines($"{path}~/{script.Sort:D8}.txt"))
                                {
                                    var m = Regex.Match(line, @"◆(\d+)◆(.+$)");
                                    if (!m.Success) continue;

                                    var index = int.Parse(m.Groups[1].Value);
                                    var text = m.Groups[2].Value;

                                    var lines = translated[index] ?? Array.Empty<string>();
                                    Array.Resize(ref lines, lines.Length + 1);
                                    lines[lines.Length - 1] = text;
                                    translated[index] = lines;
                                }

                                // ReSharper disable once InconsistentNaming
                                var _O2I = new Dictionary<int, int>();
                                var offset = 0;
                                for (var i = 0; i < script.Commands.Length; i++)
                                {
                                    _O2I.Add(offset, i);
                                    offset += script.Commands[i].Length;
                                }

                                // ReSharper disable once InconsistentNaming
                                var _I2O = new Dictionary<int, int>();
                                offset = 0;
                                for (var i = 0; i < script.Commands.Length; i++)
                                {
                                    _I2O.Add(i, offset);
                                    if (translated[i] != null)
                                    {
                                        script.Commands[i] = ImportV2001(script.Commands[i], translated[i]);
                                    }

                                    offset += script.Commands[i].Length;
                                }

                                foreach (var command in script.Commands)
                                {
                                    switch (command[0])
                                    {
                                        case 0x16:
                                        case 0x19:
                                        case 0x1A:
                                        {
                                            var o = BitConverter.ToInt32(command, 0x04);
                                            o = _I2O[_O2I[o]];
                                            BitConverter.GetBytes(o).CopyTo(command, 0x04);
                                        }
                                            break;
                                    }
                                }
                            }

                            var filename = path.PatchFileName(_encoding.WebName);
                            Console.WriteLine($"Write {filename}");
                            using var writer = new BinaryWriter(File.Create(filename));
                            writer.WriteUnknownScriptV2001(scripts);
                        }
                            break;
                        default:
                            throw new FormatException($"unsupported format: {path}");
                    }

                    break;
                default:
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  Export text : UnknownScriptTool -e [scenario.aqa|Gm05.dat] [encoding]");
                    Console.WriteLine("  Import text : UnknownScriptTool -i [scenario.aqa|Gm05.dat] [encoding]");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
            }
        }

        private static Encoding _encoding = Encoding.Default;

        private const string FileHead = "AQA ";

        private static UnknownScriptV2001[] ReadUnknownScriptV2001(this BinaryReader reader)
        {
            var count = reader.ReadInt32();
            var x04 = reader.ReadInt32();
            if (x04 != 0x0000_000C) throw new FormatException($"00000004h: 0x{x04:X8}");
            var x08 = reader.ReadInt32();
            if (x08 != x04 + count * 0x0C) throw new FormatException($"00000008h: 0x{x08:X8}");

            var scripts = new UnknownScriptV2001[count];
            for (var i = 0x00; i < count; i++)
            {
                reader.BaseStream.Position = 0x0000_000C + i * 0x0C;
                var index = reader.ReadBytes(0x0C);
                index.Rot();
                var sort = BitConverter.ToUInt32(index, 0x00);
                var size = BitConverter.ToInt32(index, 0x04);
                var offset = BitConverter.ToUInt32(index, 0x08);

                reader.BaseStream.Position = offset;
                var bytes = reader.ReadBytes(size);
                bytes.Rot();
                scripts[i] = new UnknownScriptV2001(sort, bytes);
            }

            return scripts;
        }

        private static UnknownScriptV2004[] ReadUnknownScriptV2004(this BinaryReader reader)
        {
            var head = Encoding.ASCII.GetString(reader.ReadBytes(0x04));
            if (head != FileHead) throw new NotSupportedException($"unsupported version: {head}.");
            _ = reader.ReadUInt32();
            var key = reader.ReadUInt32();
            var mask = (ushort)(((0x0065 * key + 0x0309) & 0xFFFF) + 0x0001);
            var count = reader.ReadInt32();

            var data = 0x0000_0018 + count * 0x90;
            var scripts = new UnknownScriptV2004[count];
            for (var i = 0x00; i < count; i++)
            {
                reader.BaseStream.Position = 0x0000_0018 + i * 0x90;
                var index = reader.ReadBytes(0x90);
                index.Xor(mask);
                var name = Encoding.GetEncoding(932).GetString(index, 0x00, 0x0C).TrimEnd('\0');
                var size = BitConverter.ToInt32(index, 0x80);
                var sort = BitConverter.ToUInt32(index, 0x84);
                var offset = BitConverter.ToUInt32(index, 0x88);

                reader.BaseStream.Position = data + offset;
                var bytes = reader.ReadBytes(size);
                scripts[i] = new UnknownScriptV2004(name, sort, bytes);
            }

            return scripts;
        }

        private static void WriteUnknownScriptV2001(this BinaryWriter writer, UnknownScriptV2001[] scripts)
        {
            writer.Write(scripts.Length);
            writer.Write(0x0000_000C);
            var offset = 0x0000_000C + scripts.Length * 0x0C;
            writer.Write(offset);

            for (var i = 0; i < scripts.Length; i++)
            {
                var bytes = scripts[i].ToBytes();
                bytes.Rot();
                var index = new byte[0x0C];
                BitConverter.GetBytes(scripts[i].Sort).CopyTo(index, 0x00);
                BitConverter.GetBytes(bytes.Length).CopyTo(index, 0x04);
                BitConverter.GetBytes(offset).CopyTo(index, 0x08);
                bytes.Rot();

                writer.BaseStream.Position = 0x0000_000C + i * 0x0C;
                writer.Write(bytes);

                writer.BaseStream.Position = offset;
                writer.Write(bytes);
                offset += bytes.Length;
            }
        }

        private static void WriteUnknownScriptV2004(this BinaryWriter writer, UnknownScriptV2004[] scripts, uint key)
        {
            writer.Write(Encoding.ASCII.GetBytes(FileHead));
            writer.Write(0x0000_FFFF);
            writer.Write(key);
            writer.Write(scripts.Length);

            var mask = (ushort)(((0x0065 * key + 0x0309) & 0xFFFF) + 0x0001);
            var offset = 0x0000_0000;
            for (var i = 0; i < scripts.Length; i++)
            {
                var bytes = scripts[i].ToBytes();
                var index = new byte[0x90];
                Encoding.GetEncoding(932).GetBytes(scripts[i].Name).CopyTo(index, 0x00);
                BitConverter.GetBytes(bytes.Length).CopyTo(index, 0x80);
                BitConverter.GetBytes(scripts[i].Sort).CopyTo(index, 0x84);
                BitConverter.GetBytes(offset).CopyTo(index, 0x8C);
                index.Xor(mask);

                writer.BaseStream.Position = 0x0000_0018 + i * 0x90;
                writer.Write(index);

                writer.BaseStream.Position = 0x0000_0018 + scripts.Length * 0x90 + offset;
                writer.Write(bytes);
                offset += bytes.Length;
            }
        }

        private static string[] ExportV2001(byte[] command)
        {
            switch (command[0x00])
            {
                case 0x22:
                {
                    var len = BitConverter.ToInt32(command, 0x04);
                    while (command[0x08 + len - 0x01] == 0x00) len--;
                    return _encoding.GetString(command, 0x08, len).Split('\n');
                }
                default:
                    return Array.Empty<string>();
            }
        }

        private static string[] ExportV2004(byte[] command)
        {
            switch (BitConverter.ToUInt16(command, 0x00))
            {
                case 0x0201:
                case 0x0241:
                {
                    var len = BitConverter.ToUInt16(command, 0x02) - 0x01;
                    while (command[0x04 + len] != 0x00) len--;
                    return _encoding.GetString(command, 0x04, len).Split('\n');
                }
                case 0x027A:
                    var count = BitConverter.ToUInt32(command, 0x06) - 0x01;
                    var arr = new string[count];
                    var position = 0x0A;
                    while (BitConverter.ToUInt16(command, position) != 0x0201) position += 0x06;

                    for (var i = 0; i < count; i++)
                    {
                        var len = BitConverter.ToUInt16(command, position + 0x02) - 0x01;
                        while (command[position + 0x04 + len] != 0x00) len--;
                        arr[i] = _encoding.GetString(command, position + 0x04, len);
                        position += BitConverter.ToUInt16(command, position + 0x02) + 0x04;
                    }

                    return arr;
                default:
                    return Array.Empty<string>();
            }
        }

        private static byte[] ImportV2001(byte[] command, string[] lines)
        {
            if (_encoding.CodePage == 936)
            {
                for (var i = 0; i < lines.Length; i++)
                {
                    lines[i] = lines[i].ReplaceGbkUnsupported();
                }
            }

            switch (command[0x00])
            {
                case 0x22:
                {
                    var bytes = _encoding.GetBytes(string.Join("\n", lines));
                    var len = (bytes.Length + 0x03) & ~0x03;
                    var buffer = new byte[0x08 + len];
                    using var stream = new MemoryStream(buffer);
                    using var writer = new BinaryWriter(stream);

                    writer.Write(command, 0x00, 0x04);
                    writer.Write(len);
                    writer.Write(bytes);

                    return buffer;
                }
                default:
                    return command;
            }
        }

        private static byte[] ImportV2004(byte[] command, string[] lines)
        {
            if (_encoding.CodePage == 936)
            {
                for (var i = 0; i < lines.Length; i++)
                {
                    lines[i] = lines[i].ReplaceGbkUnsupported();
                }
            }

            switch (BitConverter.ToUInt16(command, 0x00))
            {
                case 0x0201:
                case 0x0241:
                {
                    var bytes = _encoding.GetBytes(string.Join("\n", lines));
                    var len = (bytes.Length + 0x04) & ~0x03;
                    var buffer = new byte[0x04 + len];
                    using var stream = new MemoryStream(buffer);
                    using var writer = new BinaryWriter(stream);

                    writer.Write(command, 0x00, 0x02);
                    writer.Write((ushort)len);
                    writer.Write(bytes);

                    return buffer;
                }
                case 0x027A:
                {
                    var count = BitConverter.ToUInt32(command, 0x06) - 0x01;
                    if (count != lines.Length) throw new FormatException("Invalid number of lines");
                    var position = 0x0A;
                    while (BitConverter.ToUInt16(command, position) != 0x0201) position += 0x06;

                    var buffer = new byte[position + lines.Sum(line => (_encoding.GetByteCount(line) + 0x08) & ~0x03)];

                    using var stream = new MemoryStream(buffer);
                    using var writer = new BinaryWriter(stream);

                    writer.Write(command, 0x00, position);
                    stream.Position = 0x0000_0002;
                    writer.Write(buffer.Length);
                    foreach (var line in lines)
                    {
                        stream.Position = position;
                        var bytes = _encoding.GetBytes(line);
                        var len = (bytes.Length + 0x04) & ~0x03;
                        writer.Write((ushort)0x0201);
                        writer.Write((ushort)len);
                        writer.Write(bytes);
                        position += 0x04 + len;
                    }

                    return buffer;
                }
                default:
                    return command;
            }
        }

        private static bool HasText(this UnknownScriptV2001 v2001)
        {
            return v2001.Commands
                .Any(command => command[0x00] == 0x22);
        }

        private static bool HasText(this UnknownScriptV2004 v2004)
        {
            return v2004.Commands
                .Select(command => BitConverter.ToUInt16(command, 0x00))
                .Any(instruction => instruction == 0x0241 || instruction == 0x0201 || instruction == 0x027A);
        }
    }
}