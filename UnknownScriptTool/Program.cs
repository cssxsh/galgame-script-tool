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
        public static void Main(string[] args)
        {
            var mode = "";
            var path = "scenario.aqa";
            switch (args.Length)
            {
                case 1:
                    _encoding = null;
                    switch (args[0])
                    {
                        case "-e":
                        case "-i":
                            mode = args[0];
                            break;
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

            var scripts = Array.Empty<UnknownScript>();
            switch (mode)
            {
                case "-e":
                    _encoding ??= Encoding.GetEncoding("SHIFT-JIS");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                    using (var stream = File.OpenRead(path))
                    using (var reader = new BinaryReader(stream))
                    {
                        scripts = reader.ReadUnknownScripts();
                    }

                    Directory.CreateDirectory($"{path}~");

                    foreach (var script in scripts)
                    {
                        if (!script.HasText()) continue;
                        Console.WriteLine($"Export {script.Name}");
                        using var writer = File.CreateText($"{path}~/{script.Name}.txt");
                        for (var i = 0; i < script.Commands.Length; i++)
                        {
                            foreach (var line in Export(script.Commands[i]))
                            {
                                writer.WriteLine($">{BitConverter.ToUInt16(script.Commands[i], 0x00):X04}");
                                writer.WriteLine($"◇{i:D4}◇{line}");
                                writer.WriteLine($"◆{i:D4}◆{line}");
                                writer.WriteLine();
                            }
                        }
                    }

                    break;
                case "-i":
                    _encoding ??= Encoding.GetEncoding("GBK");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                    var key = 0x0000_0000u;
                    using (var stream = File.OpenRead(path))
                    using (var reader = new BinaryReader(stream))
                    {
                        scripts = reader.ReadUnknownScripts();
                        stream.Position = 0x0000_0008;
                        key |= reader.ReadUInt32();
                    }

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
                            if (translated[i] != null) script.Commands[i] = Import(script.Commands[i], translated[i]);
                            offset += script.Commands[i].Length;
                        }

                        foreach (var command in script.Commands)
                        {
                            if (command[0] != 0x0D && command[0] != 0x21) continue;
                            var o = BitConverter.ToInt32(command, 0x02);
                            o = _I2O[_O2I[o]];
                            BitConverter.GetBytes(o).CopyTo(command, 0x02);
                        }
                    }

                    var filename = $"{Path.GetFileNameWithoutExtension(path)}_{_encoding.WebName}.aqa";
                    Console.WriteLine($"Write {filename}");
                    using (var stream = File.Create(filename))
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.WriteUnknownScripts(scripts, key);
                    }

                    break;
                default:
                    Array.Resize(ref scripts, 0);
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  Export text : UnknownScriptTool -e [ISF] [encoding]");
                    Console.WriteLine("  Import text : UnknownScriptTool -i [ISF] [encoding]");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
            }
        }

        private static Encoding _encoding = Encoding.Default;

        private const string FileHead = "AQA ";

        private static UnknownScript[] ReadUnknownScripts(this BinaryReader reader)
        {
            var head = _encoding.GetString(reader.ReadBytes(0x04));
            if (head != FileHead) throw new NotSupportedException($"Not supported version: {head}.");
            _ = reader.ReadUInt32();
            var key = reader.ReadUInt32();
            var mask = (ushort)(((0x0065 * key + 0x0309) & 0xFFFF) + 0x0001);
            var count = reader.ReadInt32();

            reader.BaseStream.Position = 0x0000_0018;
            var index = reader.ReadBytes(count * 0x90);
            for (var i = 0; i < index.Length; i++)
            {
                index[i] ^= (byte)((i & 0x01) == 0x00 ? mask : mask >> 0x08);
            }

            var data = 0x0000_0018 + index.Length;
            using var s = new MemoryStream(index);
            using var r = new BinaryReader(s);
            var scripts = new UnknownScript[count];
            for (var i = 0; i < count; i++)
            {
                s.Position = i * 0x90;
                var name = _encoding.GetString(r.ReadBytes(0x80).TrimEnd());
                var size = r.ReadInt32();
                var sort = r.ReadUInt32();
                var offset = data + r.ReadUInt32();

                reader.BaseStream.Position = offset;
                var bytes = reader.ReadBytes(size);
                scripts[i] = new UnknownScript(name, sort, bytes);
            }

            return scripts;
        }

        private static void WriteUnknownScripts(this BinaryWriter writer, UnknownScript[] scripts, uint key)
        {
            writer.Write(_encoding.GetBytes(FileHead));
            writer.Write(0x0000_FFFF);
            writer.Write(key);
            writer.Write(scripts.Length);

            var index = new byte[scripts.Length * 0x90];
            using var s = new MemoryStream(index);
            using var w = new BinaryWriter(s);
            var data = 0x0000_0018 + index.Length;
            writer.BaseStream.Position = data;
            for (var i = 0; i < scripts.Length; i++)
            {
                var bytes = scripts[i].ToBytes();

                s.Position = i * 0x90;
                w.Write(_encoding.GetBytes(scripts[i].Name));
                s.Position = i * 0x90 + 0x80;
                w.Write(bytes.Length);
                w.Write(scripts[i].Sort);
                w.Write((uint)(writer.BaseStream.Position - data));

                writer.Write(bytes);
            }

            var mask = (ushort)(((0x0065 * key + 0x0309) & 0xFFFF) + 0x0001);
            for (var i = 0; i < index.Length; i++)
            {
                index[i] ^= (byte)((i & 0x01) == 0x00 ? mask : mask >> 0x08);
            }

            writer.BaseStream.Position = 0x0000_0018;
            writer.Write(index);
        }

        private static string[] Export(byte[] command)
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

        private static byte[] Import(byte[] command, string[] lines)
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
                    using var steam = new MemoryStream(buffer);
                    using var writer = new BinaryWriter(steam);

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

                    using var steam = new MemoryStream(buffer);
                    using var writer = new BinaryWriter(steam);

                    writer.Write(command, 0x00, position);
                    steam.Position = 0x0000_0002;
                    writer.Write(buffer.Length);
                    foreach (var line in lines)
                    {
                        steam.Position = position;
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

        private static bool HasText(this UnknownScript script)
        {
            return script.Commands
                .Select(command => BitConverter.ToUInt16(command, 0x00))
                .Any(instruction => instruction == 0x0241 || instruction == 0x0201 || instruction == 0x027A);
        }
    }
}