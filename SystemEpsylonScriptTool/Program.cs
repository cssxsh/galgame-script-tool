﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATool;

namespace SystemEpsylon
{
    internal static class Program
    {
        public static void Main(params string[] args)
        {
            var mode = "";
            var path = "script.dat";
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
                    using var reader = new BinaryReader(File.OpenRead(path));
                    var scripts = reader.ReadSystemEpsylonScripts();

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
                            writer.WriteLine($">{script.Commands[i][0]:X2}");
                            writer.WriteLine($"◇{i:D4}◇{text}");
                            writer.WriteLine($"◆{i:D4}◆{text}");
                            writer.WriteLine();
                        }
                    }
                }
                    break;
                case "-i":
                    _encoding ??= Encoding.GetEncoding("GBK");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                {
                    using var reader = new BinaryReader(File.OpenRead(path));
                    var scripts = reader.ReadSystemEpsylonScripts();

                    foreach (var script in scripts)
                    {
                        if (!script.HasText()) continue;
                        if (!File.Exists($"{path}~/{script.Name}.txt")) continue;
                        Console.WriteLine($"Import {script.Name}");
                        var translated = new string[script.Commands.Length][];
                        foreach (var line in File.ReadLines($"{path}~/{script.Name}.txt"))
                        {
                            var match = Regex.Match(line, @"◆(\d+)◆(.+)$");
                            if (!match.Success) continue;

                            var index = int.Parse(match.Groups[1].Value);
                            var text = match.Groups[2].Value;

                            var lines = translated[index] ?? Array.Empty<string>();
                            Array.Resize(ref lines, lines.Length + 1);
                            lines[lines.Length - 1] = text;
                            translated[index] = lines;
                        }

                        for (var i = 0; i < script.Commands.Length; i++)
                        {
                            if (translated[i] == null) continue;
                            script.Commands[i] = Import(script.Commands[i], string.Join("\n", translated[i]));
                        }
                    }

                    var filename = path.PatchFileName(_encoding.WebName);
                    Console.WriteLine($"Write {filename}");
                    using var writer = new BinaryWriter(File.Create(filename));
                    writer.WriteSystemEpsylonScripts(scripts);
                }
                    break;
                default:
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  Export text : SystemEpsylonTool -e [script.dat] [encoding]");
                    Console.WriteLine("  Import text : SystemEpsylonTool -i [script.dat] [encoding]");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
            }
        }

        private static Encoding _encoding = Encoding.Default;

        private const string FileHead = "PACKDAT.";

        private static SystemEpsylonScript[] ReadSystemEpsylonScripts(this BinaryReader reader)
        {
            var head = Encoding.ASCII.GetString(reader.ReadBytes(0x08));
            if (head != FileHead) throw new NotSupportedException($"unsupported version: {head}.");
            var count = reader.ReadInt32();
            var scripts = new SystemEpsylonScript[count];

            for (var i = 0x00; i < count; i++)
            {
                reader.BaseStream.Position = 0x0000_0010 + i * 0x30;
                var name = Encoding.GetEncoding(932).GetString(reader.ReadBytes(0x20)).TrimEnd('\0');
                var offset = reader.ReadUInt32();
                var flags = reader.ReadUInt32();
                var size = reader.ReadInt32();
                _ = reader.ReadUInt32();

                reader.BaseStream.Position = offset;
                var bytes = reader.ReadBytes(size);

                scripts[i] = new SystemEpsylonScript(name, flags, bytes);
            }

            return scripts;
        }

        private static void WriteSystemEpsylonScripts(this BinaryWriter writer, SystemEpsylonScript[] scripts)
        {
            writer.Write(Encoding.ASCII.GetBytes(FileHead));
            writer.Write(scripts.Length);
            writer.Write(scripts.Length);
            var buffer = new byte[0x20];
            var offset = (uint)(0x10 + scripts.Length * 0x30);

            for (var i = 0x00; i < scripts.Length; i++)
            {
                var bytes = scripts[i].ToBytes();

                writer.BaseStream.Position = 0x0000_0010 + i * 0x30;
                Array.Clear(buffer, 0, buffer.Length);
                Encoding.GetEncoding(932).GetBytes(scripts[i].Name).CopyTo(buffer, 0);
                writer.Write(buffer);
                writer.Write(offset);
                writer.Write(scripts[i].Flags);
                writer.Write(bytes.Length);
                writer.Write(scripts[i].Commands.Sum(command => command.Length));

                writer.BaseStream.Position = offset;
                writer.Write(bytes);
                offset += (uint)(bytes.Length + 0x03) & ~0x03u;
                var empty = new byte[offset - writer.BaseStream.Position];
                writer.Write(empty);
            }
        }

        private static string Export(byte[] command)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (command[0x00])
            {
                case 0x00:
                    var count = 0x00;
                    for (var i = command[0x01]; i < command.Length; i++)
                    {
                        if (command[i] == 0x00) break;
                        count++;
                    }

                    return _encoding.GetString(command, command[0x01], count);
                default:
                    return null;
            }
        }

        private static byte[] Import(byte[] command, string text)
        {
            if (_encoding.CodePage == 936) text = text.ReplaceGbkUnsupported();
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (command[0x00])
            {
                case 0x00:
                    var bytes = _encoding.GetBytes(text);
                    if (bytes.Length + 0x01 > command[0x03])
                    {
                        var size = (byte)((bytes.Length + 0x04) & ~0x03u);
                        command[0x03] = size;
                        Array.Resize(ref command, command[0x01] + size);
                    }

                    Array.Clear(command, command[0x01], command[0x03]);
                    bytes.CopyTo(command, command[0x01]);
                    break;
                default:
                    throw new NotSupportedException($"Import {command[0x00]:X2} is unsupported");
            }

            return command;
        }

        private static bool HasText(this SystemEpsylonScript script)
        {
            return script.Commands
                .Any(command => command.Length > 0x01 && command[0x00] == 0x00);
        }
    }
}