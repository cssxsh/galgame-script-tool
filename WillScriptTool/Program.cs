using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATool;

namespace Will
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var mode = "";
            var path = "*.scb";
            switch (args.Length)
            {
                case 1:
                    _encoding = null;
                    switch (args[0])
                    {
                        case "-e":
                        case "-i":
                            mode = args[0];
                            path = Directory
                                .EnumerateFiles(".", "*.scb", SearchOption.TopDirectoryOnly)
                                .DefaultIfEmpty(".scb")
                                .First();
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

            var scripts = Array.Empty<WillScript>();
            switch (mode)
            {
                case "-e":
                    _encoding ??= Encoding.GetEncoding("SHIFT-JIS");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                    using (var stream = File.OpenRead(path))
                    using (var reader = new BinaryReader(stream, _jis))
                    {
                        scripts = reader.ReadWillScripts();
                    }

                    Directory.CreateDirectory($"{path}~");

                    foreach (var script in scripts)
                    {
                        if (!script.HasText()) continue;
                        Console.WriteLine($"Export {script.Name}");
                        using var writer = File.CreateText($"{path}~/{script.Name}.txt");
                        for (var i = 0; i < script.Commands.Length; i++)
                        {
                            var text = Export(script.Commands[i]);
                            if (text == null) continue;
                            writer.WriteLine($">{script.Commands[i][1]:X2}");
                            writer.WriteLine($"◇{i:D4}◇{text}");
                            writer.WriteLine($"◆{i:D4}◆{text}");
                            writer.WriteLine();
                        }
                    }

                    break;
                case "-i":
                    _encoding ??= Encoding.GetEncoding("GBK");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                    using (var stream = File.OpenRead(path))
                    using (var reader = new BinaryReader(stream))
                    {
                        scripts = reader.ReadWillScripts();
                    }

                    foreach (var script in scripts)
                    {
                        if (!script.HasText()) continue;
                        if (!File.Exists($"{path}~/{script.Name}.txt")) continue;
                        Console.WriteLine($"Import {script.Name}");
                        var translated = new string[script.Commands.Length];
                        foreach (var line in File.ReadLines($"{path}~/{script.Name}.txt"))
                        {
                            var match = Regex.Match(line, @"◆(\d+)◆(.+)$");
                            if (!match.Success) continue;

                            var index = int.Parse(match.Groups[1].Value);
                            var text = match.Groups[2].Value;

                            translated[index] = text;
                        }

                        for (var i = 0; i < script.Commands.Length; i++)
                        {
                            if (translated[i] == null) continue;
                            script.Commands[i] = Import(script.Commands[i], translated[i]);
                        }
                    }

                    var filename =
                        $"{Path.GetFileNameWithoutExtension(path)}_{_encoding.WebName}{Path.GetExtension(path)}";
                    Console.WriteLine($"Write {filename}");
                    using (var stream = File.Create(filename))
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.WriteWillScripts(scripts);
                    }

                    break;
                default:
                    Array.Resize(ref scripts, 0);
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  Export text : WillTool -e [*.scb] [encoding]");
                    Console.WriteLine("  Import text : WillTool -i [*.scb] [encoding]");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
            }
        }

        private static Encoding _encoding = Encoding.Default;

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private static Encoding _jis = Encoding.GetEncoding("SHIFT-JIS");

        private const string FileHead = "ARCG";

        private static WillScript[] ReadWillScripts(this BinaryReader reader)
        {
            var head = _encoding.GetString(reader.ReadBytes(4));
            if (head != FileHead) throw new NotSupportedException($"Not supported version: {head}.");
            var version = reader.ReadUInt32();
            if (version != 0x0001_0000u) throw new NotSupportedException($"Not supported version: {version:X8}.");
            var offset = reader.ReadUInt32(); // index offset
            _ = reader.ReadInt32(); // index size
            var folders = reader.ReadUInt16();
            if (folders != 0x0000_0001u) throw new NotSupportedException($"Not supported folders: {folders}.");
            var files = reader.ReadUInt32();
            var scripts = new WillScript[files];

            reader.BaseStream.Position = offset;
            // var folder = _jis.GetString(reader.ReadBytes(reader.ReadByte() - 1).TrimEnd());
            // if (folder.Length != 0) throw new NotSupportedException($"Not supported folder: {folder}.");
            var folder = reader.ReadUInt32();
            if (folder != 0x0000_0004) throw new NotSupportedException($"Not supported folder: {folder:X8}.");
            offset = reader.ReadUInt32();
            _ = reader.ReadUInt32(); // folder files count

            reader.BaseStream.Position = offset;
            for (var i = 0; i < files; i++)
            {
                var len = reader.ReadByte() - 1;
                var name = _jis.GetString(reader.ReadBytes(len).TrimEnd());
                offset = reader.ReadUInt32(); // file offset
                var size = reader.ReadInt32(); // size offset

                var position = reader.BaseStream.Position;
                reader.BaseStream.Position = offset;
                var bytes = reader.ReadBytes(size);
                // Console.WriteLine($"file: {name.ReplaceGbkUnsupported()}");

                scripts[i] = new WillScript(name, bytes);
                reader.BaseStream.Position = position;
            }

            return scripts;
        }

        private static void WriteWillScripts(this BinaryWriter writer, WillScript[] scripts)
        {
            writer.Write(Encoding.ASCII.GetBytes(FileHead));
            writer.Write(0x0001_0000u);
            writer.Write(0xFFFF_FFFFu);
            writer.Write(0xFFFF_FFFFu);
            writer.Write((ushort)1);
            writer.Write((uint)scripts.Length);

            writer.BaseStream.Position = 0x20;
            for (var i = 0; i < scripts.Length; i++)
            {
                var bytes = scripts[i].ToBytes();
                writer.Write(bytes);
            }

            var offset = writer.BaseStream.Position;
            writer.Write(0x0000_0004u);
            writer.Write((uint)(writer.BaseStream.Position + 0x0C));
            writer.Write((uint)scripts.Length);
            writer.Write(0x0000_0000u);

            var position = 0x0000_0020u;
            for (var i = 0; i < scripts.Length; i++)
            {
                var bytes = _jis.GetBytes(scripts[i].Name);
                var len = (bytes.Length + 2 + 3) & 0xFFFF_FFFCu;
                Array.Resize(ref bytes, (int)(len - 1));
                writer.Write((byte)len);
                writer.Write(bytes);
                var size = (uint)scripts[i].Commands.Sum(command => command.Length);
                writer.Write(position);
                writer.Write(size);
                position += size;
            }

            writer.Write(0x0000_0000u);
            var diff = writer.BaseStream.Position - offset;
            writer.BaseStream.Position = 0x0000_0008;
            writer.Write((uint)offset);
            writer.Write((uint)diff);
        }

        private static string Export(byte[] command)
        {
            if (command.Length != command[0]) return null;
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (command[1])
            {
                // Message
                case 0x09:
                {
                    return _encoding.GetString(command, 2, command[0] - 3);
                }
                // Character Name
                case 0x25:
                {
                    var offset = 2;
                    for (; offset < 8; offset++)
                        if (command[offset] == 0x00)
                            break;

                    return _encoding.GetString(command, 2, offset - 2);
                }
                default:
                    return null;
            }
        }

        private static byte[] Import(byte[] command, string text)
        {
            if (command.Length != command[0]) return command;
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (command[1])
            {
                // Message
                case 0x09:
                {
                    var bytes = AsMessage();

                    Array.Resize(ref command, 2 + bytes.Length + 1);
                    command[0] = (byte)command.Length;
                    Array.Clear(command, 2, command.Length - 2);
                    bytes.CopyTo(command, 2);
                }
                    break;
                // Character Name
                case 0x25:
                {
                    if (_encoding.CodePage == 936) text = text.ReplaceGbkUnsupported();
                    var bytes = _encoding.GetBytes(text);
                    Array.Clear(command, 2, command.Length - 2);
                    bytes.CopyTo(command, 2);
                }
                    break;
            }

            return command;

            byte[] AsMessage()
            {
                var match = Regex.Match(text, @"(\[\d{4}.+\])|([^[]+)", RegexOptions.Multiline);
                switch (_encoding.CodePage)
                {
                    case 932:
                        return _encoding.GetBytes(text);
                    case 936:
                        var bytes = new byte[_encoding.GetByteCount(text.ReplaceGbkUnsupported())];
                        var index = 0;
                        while (match.Success)
                        {
                            var temp = match.Groups[1].Success
                                ? _jis.GetBytes(match.Groups[1].Value)
                                : _encoding.GetBytes(match.Groups[2].Value.ReplaceGbkUnsupported());
                            temp.CopyTo(bytes, index);
                            index += temp.Length;
                            match = match.NextMatch();
                        }

                        return bytes;
                    default:
                        var buffer = new List<byte>();
                        while (match.Success)
                        {
                            var temp = match.Groups[1].Success
                                ? _jis.GetBytes(match.Groups[1].Value)
                                : _encoding.GetBytes(match.Groups[2].Value);
                            buffer.AddRange(temp);
                            match = match.NextMatch();
                        }

                        return buffer.ToArray();
                }
            }
        }

        private static bool HasText(this WillScript script)
        {
            if (script.Name.EndsWith("Tbl")) return false;
            return script.Commands
                .Any(command => command.Length > 2 && (command[1] == 0x09 || command[1] == 0x25));
        }
    }
}