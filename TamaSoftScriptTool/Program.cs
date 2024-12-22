using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATool;

namespace TamaSoft
{
    internal static class Program
    {
        public static void Main(params string[] args)
        {
            var mode = "";
            var path = "data.epk";
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

            switch (mode)
            {
                case "-e":
                    _encoding ??= Encoding.GetEncoding("SHIFT-JIS");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                {
                    var sources = new List<FileStream> { File.OpenRead(path) };
                    var pattern = $"{Path.GetFileNameWithoutExtension(path)}.e*";
                    sources.AddRange(Directory
                        .EnumerateFiles(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".", pattern)
                        .Select(File.OpenRead));
                    using var reader = new BinaryReader(new MultiFileStream(sources.ToArray()), Encoding.ASCII, true);
                    var scripts = reader.ReadTamaSoftScripts();

                    Directory.CreateDirectory($"{path}~");

                    foreach (var script in scripts)
                    {
                        if (!script.HasText()) continue;
                        Console.WriteLine($"Export {script.Name}");
                        Directory.CreateDirectory(Path.GetDirectoryName($"{path}~/{script.Name}") ?? "data");
                        using var writer = File.CreateText($"{path}~/{script.Name}.txt");
                        for (var i = 0; i < script.Commands.Length; i++)
                        {
                            var instruction = BitConverter.ToUInt32(script.Commands[i], 0x00);
                            foreach (var line in Export(script.Commands[i], script.Key))
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
                case "-i":
                    _encoding ??= Encoding.GetEncoding("GBK");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                {
                    var sources = new List<FileStream> { File.OpenRead(path) };
                    var pattern = $"{Path.GetFileNameWithoutExtension(path)}.e*";
                    sources.AddRange(Directory
                        .EnumerateFiles(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".", pattern)
                        .Select(File.OpenRead));
                    using var reader = new BinaryReader(new MultiFileStream(sources.ToArray()), Encoding.ASCII, true);
                    var scripts = reader.ReadTamaSoftScripts();

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

                        for (var i = 0; i < script.Commands.Length; i++)
                        {
                            if (translated[i] == null) continue;
                            script.Commands[i] = Import(script.Commands[i], translated[i], script.Key);
                        }
                    }

                    var filename = path.PatchFileName(_encoding.WebName);
                    Console.WriteLine($"Write {filename}");
                    using var writer = new BinaryWriter(File.Create(filename), Encoding.ASCII, true);
                    writer.WriteTamaSoftScripts(scripts);
                }
                    break;
                default:
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  Export text : TamaSoftScriptTool -e [data.epk] [encoding]");
                    Console.WriteLine("  Import text : TamaSoftScriptTool -i [data.epk] [encoding]");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
            }
        }

        private static Encoding _encoding = Encoding.Default;

        private const string FileHead = "EPK ";

        private static TamaSoftScript[] ReadTamaSoftScripts(this BinaryReader reader)
        {
            var head = Encoding.ASCII.GetString(reader.ReadBytes(0x04));
            if (head != FileHead) throw new NotSupportedException($"unsupported version: {head}.");
            _ = reader.ReadUInt32(); // 0x20 + count * 0x28
            _ = reader.ReadUInt64(); // packages size
            _ = reader.ReadUInt64(); // file size
            var count = reader.ReadUInt32();
            var scripts = new List<TamaSoftScript>();

            for (var i = 0x0000u; i < count; i++)
            {
                reader.BaseStream.Position = 0x0000_0020 + i * 0x28;
                _ = reader.ReadUInt32(); // 0x0013_0178
                _ = reader.ReadUInt32(); // 0x0000_0000
                var offsetOfName = reader.ReadUInt32();
                _ = reader.ReadUInt32(); // 0x0013_3E88 / 0x0013_3EA0
                var offsetOfData = reader.ReadInt64(); // offset
                var size = reader.ReadInt32(); // size
                _ = reader.ReadUInt32(); // 0x0000_0000
                _ = DateTime.FromFileTimeUtc(reader.ReadInt64());

                reader.BaseStream.Position = offsetOfName;
                var len = reader.ReadInt32();
                var raw = reader.ReadBytes(len);
                for (var j = 0; j < raw.Length; ++j) raw[j] ^= 0xFF;
                var name = Encoding.GetEncoding(932).GetString(raw);
                if (!name.ToUpperInvariant().EndsWith(".SNR")) continue;

                reader.BaseStream.Position = offsetOfData;
                var bytes = reader.ReadBytes(size);
                scripts.Add(new TamaSoftScript(name, bytes));
            }

            return scripts.ToArray();
        }

        private static void WriteTamaSoftScripts(this BinaryWriter writer, TamaSoftScript[] scripts)
        {
            var offsetOfName = 0x0000_0020L + scripts.Length * 0x28;
            var offsetOfData = offsetOfName + scripts
                .Sum(script => Encoding.GetEncoding(932).GetByteCount(script.Name) + 0x05);
            var time = DateTime.Now.ToFileTimeUtc();

            writer.Write(Encoding.ASCII.GetBytes(FileHead));
            writer.Write((uint)offsetOfName);
            writer.Write(offsetOfData);
            writer.Write(offsetOfData);
            writer.Write(scripts.Length);

            for (var i = 0; i < scripts.Length; i++)
            {
                var name = Encoding.GetEncoding(932).GetBytes(scripts[i].Name);
                for (var j = 0; j < name.Length; ++j) name[j] ^= 0xFF;
                var bytes = scripts[i].ToBytes();

                writer.BaseStream.Position = 0x0000_0020 + i * 0x28;
                writer.Write(0x0013_0178L);
                writer.Write(offsetOfName);
                writer.Write(offsetOfData);
                writer.Write((ulong)bytes.Length);
                writer.Write(time);

                writer.BaseStream.Position = offsetOfName;
                writer.Write(name.Length);
                writer.Write(name);

                writer.BaseStream.Position = offsetOfData;
                writer.Write(bytes);

                offsetOfName += 0x04 + name.Length + 0x01;
                offsetOfData += bytes.Length;
            }

            writer.BaseStream.Position = 0x0000_0008;
            writer.Write(offsetOfData);
            writer.Write(offsetOfData);
        }

        private static string[] Export(byte[] command, uint key)
        {
            using var stream = new MemoryStream(command);
            using var reader = new BinaryReader(stream);
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (reader.ReadUInt32())
            {
                case 0x0000_0101:
                {
                    var count = reader.ReadInt32();
                    var raw = reader.ReadBytes(count);
                    TamaSoftSecret.Handle(raw, key);
                    return new[] { _encoding.GetString(raw) };
                }
                case 0x0000_0303:
                {
                    _ = reader.ReadInt32();
                    var lines = new string[0x03];
                    for (var i = 0x00; i < lines.Length; i++)
                    {
                        var count = reader.ReadInt32();
                        var raw = reader.ReadBytes(count);
                        TamaSoftSecret.Handle(raw, key);
                        lines[i] = _encoding.GetString(raw);
                    }

                    return lines[0x00].Length == 0x00
                        ? new[] { lines[0x00], lines[0x02] }
                        : new[] { lines[0x02] };
                }
                case 0x0000_0605:
                {
                    var lines = new string[0x04];
                    for (var i = 0x00; i < lines.Length; i++)
                    {
                        var count = reader.ReadInt32();
                        var raw = reader.ReadBytes(count);
                        _ = reader.ReadInt32();
                        TamaSoftSecret.Handle(raw, key);
                        lines[i] = _encoding.GetString(raw);
                    }

                    return lines.Where(line => line != "").ToArray();
                }
                default:
                    return Array.Empty<string>();
            }
        }

        private static byte[] Import(byte[] command, string[] lines, uint key)
        {
            if (_encoding.CodePage == 936)
            {
                for (var i = 0; i < lines.Length; i++)
                {
                    lines[i] = lines[i].ReplaceGbkUnsupported();
                }
            }

            using var stream = new MemoryStream(command);
            using var reader = new BinaryReader(stream);
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (reader.ReadUInt32())
            {
                case 0x0000_0101:
                {
                    var text = _encoding.GetBytes(lines.Single());
                    TamaSoftSecret.Handle(text, key);
                    var buffer = new byte[0x08 + text.Length];
                    using var s = new MemoryStream(buffer);
                    using var w = new BinaryWriter(s);
                    w.Write(0x0000_0101);
                    w.Write(text.Length);
                    w.Write(text);
                    return buffer;
                }
                case 0x0000_0303:
                {
                    var index = reader.ReadInt32();
                    _ = reader.ReadBytes(reader.ReadInt32());
                    var picture = reader.ReadBytes(reader.ReadInt32());
                    _ = reader.ReadBytes(reader.ReadInt32());
                    var name = lines.Length == 0x01 ? Array.Empty<byte>() : _encoding.GetBytes(lines[0]);
                    var text = _encoding.GetBytes(lines.Last());
                    TamaSoftSecret.Handle(text, key);
                    var buffer = new byte[0x14 + name.Length + picture.Length + text.Length];
                    using var s = new MemoryStream(buffer);
                    using var w = new BinaryWriter(s);
                    w.Write(0x0000_0303);
                    w.Write(index);
                    w.Write(name.Length);
                    w.Write(name);
                    w.Write(picture.Length);
                    w.Write(picture);
                    w.Write(text.Length);
                    w.Write(text);
                    return buffer;
                }
                case 0x0000_0605:
                {
                    var buffer = new byte[0x24 + lines.Sum(line => _encoding.GetByteCount(line))];
                    using var s = new MemoryStream(buffer);
                    using var w = new BinaryWriter(s);
                    w.Write(0x0000_0605);
                    for (var i = 0x00; i < 0x04; i++)
                    {
                        var count = reader.ReadInt32();
                        var text = reader.ReadBytes(count);
                        var value = reader.ReadInt32();
                        if (i < lines.Length)
                        {
                            text = _encoding.GetBytes(lines[i]);
                            TamaSoftSecret.Handle(text, key);
                        }

                        w.Write(text.Length);
                        w.Write(text);
                        w.Write(value);
                    }
                }
                    break;
            }

            return command;
        }

        private static bool HasText(this TamaSoftScript script)
        {
            return script.Commands
                .Any(command => BitConverter.ToUInt32(command, 0x00) switch
                {
                    0x0000_0101 => true,
                    0x0000_0303 => true,
                    0x0000_0605 => true,
                    0x0000_0811 => false,
                    0x0000_0848 => false,
                    0x0000_0944 => false,
                    0x0000_09D0 => false,
                    0x0000_0A20 => false,
                    0x0000_0A21 => false,
                    0x0000_0AC0 => false,
                    0x0000_0B23 => false,
                    _ => false
                });
        }
    }
}