using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATool;

namespace Ikura
{
    internal static class Program
    {
        public static void Main(params string[] args)
        {
            var mode = "";
            var path = "ISF";
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
                        case "-E":
                        case "-I":
                            mode = args[0];
                            path = "SNR";
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
                    Environment.SetEnvironmentVariable("ISF_PATH", path);
                    using var stream = File.OpenRead(path);
                    using var reader = new BinaryReader(stream);
                    var scripts = reader.ReadIkuraScripts();

                    Directory.CreateDirectory($"{path}~");

                    foreach (var script in scripts)
                    {
                        if (!script.HasText()) continue;
                        Console.WriteLine($"Export {script.Name}");
                        using var writer = File.CreateText($"{path}~/{script.Name}.txt");
                        for (var i = 0; i < script.Commands.Length; i++)
                        {
                            foreach (var line in Export(script.Commands[i].Key, script.Commands[i].Value))
                            {
                                writer.WriteLine($">{script.Commands[i].Key}");
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
                    Environment.SetEnvironmentVariable("ISF_PATH", path);
                    using var stream = File.OpenRead(path);
                    using var reader = new BinaryReader(stream);
                    var scripts = reader.ReadIkuraScripts();

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
                            var instruction = script.Commands[i].Key;
                            var bytes = Import(instruction, script.Commands[i].Value, translated[i]);
                            script.Commands[i] = new KeyValuePair<IkuraScript.Instruction, byte[]>(instruction, bytes);
                        }
                    }

                    var filename = path.PatchFileName(_encoding.WebName);
                    Console.WriteLine($"Write {filename}");
                    using var s = File.Create(filename);
                    using var w = new BinaryWriter(s);
                    w.WriteIkuraScripts(scripts);
                }
                    break;
                case "-E":
                    _encoding ??= Encoding.GetEncoding("SHIFT-JIS");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                {
                    using var stream = File.OpenRead(path);
                    using var reader = new BinaryReader(stream);
                    var scripts = reader.ReadRomanceScripts();

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
                                writer.WriteLine($">{script.Commands[i][0x00]:X2}");
                                writer.WriteLine($"◇{i:D4}◇{line}");
                                writer.WriteLine($"◆{i:D4}◆{line}");
                                writer.WriteLine();
                            }
                        }
                    }
                }
                    break;
                case "-I":
                    _encoding ??= Encoding.GetEncoding("GBK");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                {
                    using var stream = File.OpenRead(path);
                    using var reader = new BinaryReader(stream);
                    var scripts = reader.ReadRomanceScripts();

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
                            script.Commands[i] = Import(script.Commands[i], translated[i]);
                        }
                    }

                    var filename = path.PatchFileName(_encoding.WebName);
                    Console.WriteLine($"Write {filename}");
                    using var s = File.Create(filename);
                    using var w = new BinaryWriter(s);
                    w.WriteRomanceScripts(scripts);
                }
                    break;
                default:
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  Export text : IkuraScriptTool -e [ISF] [encoding]");
                    Console.WriteLine("  Import text : IkuraScriptTool -i [ISF] [encoding]");
                    Console.WriteLine("  Export text : IkuraScriptTool -E [SNR] [encoding]");
                    Console.WriteLine("  Import text : IkuraScriptTool -I [SNR] [encoding]");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
            }
        }

        private static Encoding _encoding = Encoding.Default;

        private const string FileHead = "SM2MPX10";

        private static IkuraScript[] ReadIkuraScripts(this BinaryReader reader)
        {
            var head = Encoding.ASCII.GetString(reader.ReadBytes(0x08));
            if (head != FileHead) throw new NotSupportedException($"unsupported version: {head}.");
            var count = reader.ReadInt32();
            _ = reader.ReadInt32();
            var type = Encoding.ASCII.GetString(reader.ReadBytes(0x0C).TrimEnd());
            if (type != "ISF") throw new NotSupportedException($"unsupported type: {type}.");
            var offset = reader.ReadInt32();

            var scripts = new IkuraScript[count];

            for (var i = 0; i < count; i++)
            {
                reader.BaseStream.Position = offset + i * 0x14;
                var name = Encoding.GetEncoding(932).GetString(reader.ReadBytes(0x0C).TrimEnd());
                var pos = reader.ReadInt32();
                var size = reader.ReadInt32();

                reader.BaseStream.Position = pos;
                var bytes = reader.ReadBytes(size);

                scripts[i] = new IkuraScript(name, bytes);
            }

            return scripts;
        }

        private static RomanceScript[] ReadRomanceScripts(this BinaryReader reader)
        {
            var head = Encoding.ASCII.GetString(reader.ReadBytes(0x08));
            if (head != FileHead) throw new NotSupportedException($"unsupported version: {head}.");
            var count = reader.ReadInt32();
            _ = reader.ReadInt32();
            var type = Encoding.ASCII.GetString(reader.ReadBytes(0x0C).TrimEnd());
            if (type != "SNR") throw new NotSupportedException($"unsupported type: {type}.");
            var offset = reader.ReadInt32();

            var scripts = new List<RomanceScript>(count);

            for (var i = 0; i < count; i++)
            {
                reader.BaseStream.Position = offset + i * 0x14;
                var name = Encoding.GetEncoding(932).GetString(reader.ReadBytes(0x0C).TrimEnd());
                if (!name.ToUpperInvariant().EndsWith(".SNR")) continue;
                var pos = reader.ReadInt32();
                var size = reader.ReadInt32();

                reader.BaseStream.Position = pos;
                var bytes = reader.ReadBytes(size);

                scripts.Add(new RomanceScript(name, bytes));
            }

            return scripts.ToArray();
        }

        private static void WriteIkuraScripts(this BinaryWriter writer, IkuraScript[] scripts)
        {
            var offset = (uint)(0x0000_0020 + 0x14 * scripts.Length);
            offset = (offset + 0x0F) & ~0x0Fu;
            var buffer = new byte[0x0C];
            writer.Write(Encoding.ASCII.GetBytes(FileHead));
            writer.Write(scripts.Length);
            writer.Write(offset - 0x04);

            Array.Clear(buffer, 0x00, buffer.Length);
            Encoding.ASCII.GetBytes("ISF").CopyTo(buffer, 0x00);
            writer.Write(buffer);
            writer.Write(0x0000_0020);

            for (var i = 0; i < scripts.Length; i++)
            {
                var bytes = scripts[i].ToBytes();

                writer.BaseStream.Position = 0x0000_0020 + i * 0x14;
                Array.Clear(buffer, 0x00, buffer.Length);
                Encoding.GetEncoding(932).GetBytes(scripts[i].Name).CopyTo(buffer, 0x00);
                writer.Write(buffer);
                writer.Write(offset);
                writer.Write((uint)bytes.Length);

                writer.BaseStream.Position = offset;
                writer.Write(bytes);
                offset += (uint)(bytes.Length + 0x0F) & ~0x0Fu;
                var empty = new byte[offset - writer.BaseStream.Position];
                writer.Write(empty);
            }
        }

        private static void WriteRomanceScripts(this BinaryWriter writer, RomanceScript[] scripts)
        {
            var offset = (uint)(0x0000_0020 + 0x14 * scripts.Length);
            offset = (offset + 0x0F) & ~0x0Fu;
            var buffer = new byte[0x0C];
            writer.Write(Encoding.ASCII.GetBytes(FileHead));
            writer.Write(scripts.Length);
            writer.Write(offset - 0x04);

            Array.Clear(buffer, 0x00, buffer.Length);
            Encoding.ASCII.GetBytes("SNR").CopyTo(buffer, 0x00);
            writer.Write(buffer);
            writer.Write(0x0000_0020);

            for (var i = 0; i < scripts.Length; i++)
            {
                var bytes = scripts[i].ToBytes();

                writer.BaseStream.Position = 0x0000_0020 + i * 0x14;
                Array.Clear(buffer, 0x00, buffer.Length);
                Encoding.GetEncoding(932).GetBytes(scripts[i].Name).CopyTo(buffer, 0x00);
                writer.Write(buffer);
                writer.Write(offset);
                writer.Write((uint)bytes.Length);

                writer.BaseStream.Position = offset;
                writer.Write(bytes);
                offset += (uint)(bytes.Length + 0x0F) & ~0x0Fu;
                var empty = new byte[offset - writer.BaseStream.Position];
                writer.Write(empty);
            }
        }

        private static string[] Export(IkuraScript.Instruction instruction, byte[] args)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (instruction)
            {
                case IkuraScript.Instruction.CSET:
                    return new[] { _encoding.GetString(args, 18, args.Length - 18) };
                case IkuraScript.Instruction.CNS:
                    return new[] { _encoding.GetString(args, 2, args.Length - 2) };
                case IkuraScript.Instruction.PM:
                case IkuraScript.Instruction.PMP:
                    return IkuraScript.Decode(args, 1)
                        .Select(line => _encoding.GetString(line))
                        .ToArray();
                case IkuraScript.Instruction.MSGBOX:
                    return new[] { _encoding.GetString(args, 4, args.Length - 6) };
                case IkuraScript.Instruction.MPM:
                    if (args[1] == 0) return Array.Empty<string>();
                    return IkuraScript.Decode(args, 2)
                        .Select(line => _encoding.GetString(line))
                        .ToArray();
                case IkuraScript.Instruction.SETGAMEINFO:
                    return new[] { _encoding.GetString(args, 0, args.Length - 1) };
                default:
                    return Array.Empty<string>();
            }
        }

        private static string[] Export(byte[] command)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (command[0x00])
            {
                case 0x11:
                {
                    var prefix = command[0x01] > 0x7F ? 0x0F : 0x0E;
                    return _encoding
                        .GetString(command, prefix, command.Length - prefix - 0x02)
                        .Split('\n');
                }
                case 0x14:
                {
                    var prefix = command[0x01] > 0x7F ? 0x04 : 0x03;
                    return _encoding
                        .GetString(command, prefix, command.Length - prefix - 0x01)
                        .Split('\n');
                }
                case 0x3E:
                {
                    var prefix = command[0x01] > 0x7F ? 0x03 : 0x02;
                    return _encoding
                        .GetString(command, prefix, command.Length - prefix - 0x01)
                        .Split('\n');
                }
                default:
                    return Array.Empty<string>();
            }
        }

        private static byte[] Import(IkuraScript.Instruction instruction, byte[] args, string[] lines)
        {
            if (_encoding.CodePage == 936)
            {
                for (var i = 0; i < lines.Length; i++)
                {
                    lines[i] = lines[i].ReplaceGbkUnsupported();
                }
            }

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (instruction)
            {
                case IkuraScript.Instruction.CSET:
                {
                    var bytes = _encoding.GetBytes(lines.Single());
                    Array.Resize(ref args, 0x12 + bytes.Length);
                    bytes.CopyTo(args, 0x12);
                }
                    break;
                case IkuraScript.Instruction.CNS:
                {
                    var bytes = _encoding.GetBytes(lines.Single());
                    Array.Resize(ref args, 0x02 + bytes.Length);
                    bytes.CopyTo(args, 0x02);
                }
                    break;
                case IkuraScript.Instruction.PM:
                case IkuraScript.Instruction.PMP:
                {
                    var messages = lines.Select(line => _encoding.GetBytes(line)).ToArray();
                    args = IkuraScript.Encode(args, 0x01, messages);
                }
                    break;
                case IkuraScript.Instruction.MSGBOX:
                {
                    var bytes = _encoding.GetBytes(lines.Single());
                    var end = args.Last();
                    Array.Resize(ref args, 0x04 + bytes.Length + 0x02);
                    bytes.CopyTo(args, 0x04);
                    args[0x04 + bytes.Length] = 0x00;
                    args[0x04 + bytes.Length + 0x01] = end;
                }
                    break;
                case IkuraScript.Instruction.MPM:
                    if (args[1] == 0) break;
                {
                    var messages = lines.Select(line => _encoding.GetBytes(line)).ToArray();
                    args = IkuraScript.Encode(args, 0x02, messages);
                }
                    break;
                case IkuraScript.Instruction.SETGAMEINFO:
                {
                    var bytes = _encoding.GetBytes(lines.Single());
                    Array.Resize(ref args, 0x00 + bytes.Length + 0x01);
                    bytes.CopyTo(args, 0x00);
                    args[0x00 + bytes.Length] = 0x00;
                }
                    break;
                default:
                    throw new NotSupportedException($"Import {instruction} is unsupported");
            }

            return args;
        }

        private static byte[] Import(byte[] command, string[] lines)
        {
            var text = string.Join("\n", lines);
            if (_encoding.CodePage == 936) text = text.ReplaceGbkUnsupported();

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (command[0x00])
            {
                case 0x11:
                {
                    var bytes = _encoding.GetBytes(text);
                    if (bytes.Length + 0x0F > 0x7F)
                    {
                        var size = bytes.Length + 0x11;
                        Array.Resize(ref command, size);
                        command[0x01] = (byte)((size >> 0x08) | 0x80);
                        command[0x02] = (byte)(size & 0xFF);
                        bytes.CopyTo(command, 0x0F);
                        command[size - 0x02] = 0x00;
                        command[size - 0x01] = 0x00;
                    }
                    else
                    {
                        var size = bytes.Length + 0x10;
                        Array.Resize(ref command, size);
                        command[0x01] = (byte)(size & 0xFF);
                        bytes.CopyTo(command, 0x0E);
                        command[size - 0x02] = 0x00;
                        command[size - 0x01] = 0x00;
                    }
                }
                    break;
                case 0x14:
                {
                    var bytes = _encoding.GetBytes(text);
                    if (bytes.Length + 0x04 > 0x7F)
                    {
                        var size = bytes.Length + 0x05;
                        Array.Resize(ref command, size);
                        command[0x01] = (byte)((size >> 0x08) | 0x80);
                        command[0x02] = (byte)(size & 0xFF);
                        bytes.CopyTo(command, 0x04);
                        command[size - 0x01] = 0x00;
                    }
                    else
                    {
                        var size = bytes.Length + 0x04;
                        Array.Resize(ref command, size);
                        command[0x01] = (byte)(size & 0xFF);
                        bytes.CopyTo(command, 0x03);
                        command[size - 0x01] = 0x00;
                    }
                }
                    break;
                case 0x3E:
                {
                    var bytes = _encoding.GetBytes(text);
                    if (bytes.Length + 0x03 > 0x7F)
                    {
                        var size = bytes.Length + 0x04;
                        Array.Resize(ref command, size);
                        command[0x01] = (byte)((size >> 0x08) | 0x80);
                        command[0x02] = (byte)(size & 0xFF);
                        bytes.CopyTo(command, 0x03);
                        command[size - 0x01] = 0x00;
                    }
                    else
                    {
                        var size = bytes.Length + 0x03;
                        Array.Resize(ref command, size);
                        command[0x01] = (byte)(size & 0xFF);
                        bytes.CopyTo(command, 0x02);
                        command[size - 0x01] = 0x00;
                    }
                }
                    break;
                default:
                    throw new NotSupportedException($"Import {command[0x00]:X2} is unsupported");
            }

            return command;
        }

        private static bool HasText(this IkuraScript script)
        {
            return script.Commands
                .Any(command => command.Key switch
                {
                    IkuraScript.Instruction.CSET => true,
                    IkuraScript.Instruction.CNS => true,
                    IkuraScript.Instruction.PM => true,
                    IkuraScript.Instruction.PMP => true,
                    IkuraScript.Instruction.MSGBOX => true,
                    IkuraScript.Instruction.MPM => true,
                    IkuraScript.Instruction.SETGAMEINFO => true,
                    _ => false
                });
        }

        private static bool HasText(this RomanceScript script)
        {
            return script.Commands
                .Any(command => command[0x00] switch
                {
                    0x02 => false,
                    0x03 => false,
                    0x11 => true,
                    0x14 => true,
                    0x20 => false,
                    0x28 => false,
                    0x2C => false,
                    0x31 => false,
                    0x3D => false,
                    0x3E => true,
                    _ => false
                });
        }
    }
}