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

            var scripts = Array.Empty<IkuraScript>();
            switch (mode)
            {
                case "-e":
                    _encoding ??= Encoding.GetEncoding("SHIFT-JIS");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                    Environment.SetEnvironmentVariable("ISF_PATH", path);
                    using (var stream = File.OpenRead(path))
                    using (var reader = new BinaryReader(stream))
                    {
                        scripts = reader.ReadIkuraScripts();
                    }

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

                    break;
                case "-i":
                    _encoding ??= Encoding.GetEncoding("GBK");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                    Environment.SetEnvironmentVariable("ISF_PATH", path);
                    using (var stream = File.OpenRead(path))
                    using (var reader = new BinaryReader(stream))
                    {
                        scripts = reader.ReadIkuraScripts();
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

                        for (var i = 0; i < script.Commands.Length; i++)
                        {
                            if (translated[i] == null) continue;
                            var instruction = script.Commands[i].Key;
                            var bytes = Import(instruction, script.Commands[i].Value, translated[i]);
                            script.Commands[i] = new KeyValuePair<IkuraScript.Instruction, byte[]>(instruction, bytes);
                        }
                    }

                    var filename = $"{path}_{_encoding.WebName}";
                    Console.WriteLine($"Write {filename}");
                    using (var stream = File.Create(filename))
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.WriteIkuraScripts(scripts);
                    }

                    break;
                default:
                    Array.Resize(ref scripts, 0);
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  Export text : IkuraScriptTool -e [ISF] [encoding]");
                    Console.WriteLine("  Import text : IkuraScriptTool -i [ISF] [encoding]");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
            }
        }

        private static Encoding _encoding = Encoding.Default;

        private const string FileHead = "SM2MPX10";

        private const string FileType = "ISF";

        private static IkuraScript[] ReadIkuraScripts(this BinaryReader reader)
        {
            var head = _encoding.GetString(reader.ReadBytes(8));
            if (head != FileHead) throw new NotSupportedException($"Not supported version: {head}.");
            var count = reader.ReadInt32();
            _ = reader.ReadInt32();
            var type = _encoding.GetString(reader.ReadBytes(12).TrimEnd());
            if (type != FileType) throw new NotSupportedException($"Not supported type: {type}.");
            var offset = reader.ReadInt32();

            var scripts = new IkuraScript[count];

            for (var i = 0; i < count; i++)
            {
                reader.BaseStream.Position = offset + i * 0x14;
                var name = _encoding.GetString(reader.ReadBytes(0x0C).TrimEnd());
                var pos = reader.ReadInt32();
                var size = reader.ReadInt32();

                reader.BaseStream.Position = pos;
                var bytes = reader.ReadBytes(size);

                scripts[i] = new IkuraScript(name, bytes);
            }

            return scripts;
        }

        private static void WriteIkuraScripts(this BinaryWriter writer, IkuraScript[] scripts)
        {
            var offset = (uint)(0x0000_0020 + 0x14 * scripts.Length);
            offset = (offset + 0x0F) & ~0x0Fu;
            var buffer = new byte[0x0C];
            writer.Write(_encoding.GetBytes(FileHead));
            writer.Write(scripts.Length);
            writer.Write(offset - 0x04);

            Array.Clear(buffer, 0, buffer.Length);
            _encoding.GetBytes(FileType).CopyTo(buffer, 0);
            writer.Write(buffer);
            writer.Write(0x0000_0020);

            for (var i = 0; i < scripts.Length; i++)
            {
                var bytes = scripts[i].ToBytes();

                writer.BaseStream.Position = 0x0000_0020 + i * 0x14;
                Array.Clear(buffer, 0, buffer.Length);
                Encoding.ASCII.GetBytes(scripts[i].Name).CopyTo(buffer, 0);
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
                        .Select(line => _encoding.GetString(line).Replace("・", "﹡"))
                        .ToArray();
                case IkuraScript.Instruction.MSGBOX:
                    return new[] { _encoding.GetString(args, 4, args.Length - 6) };
                case IkuraScript.Instruction.MPM:
                    if (args[1] == 0) return Array.Empty<string>();
                    return IkuraScript.Decode(args, 2)
                        .Select(line => _encoding.GetString(line).Replace("・", "﹡"))
                        .ToArray();
                case IkuraScript.Instruction.SETGAMEINFO:
                    return new[] { _encoding.GetString(args, 0, args.Length - 1) };
                default:
                    return Array.Empty<string>();
            }
        }

        private static byte[] Import(IkuraScript.Instruction instruction, byte[] args, string[] lines)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (instruction)
            {
                case IkuraScript.Instruction.CSET:
                {
                    var bytes = _encoding.GetBytes(lines.Single());
                    Array.Resize(ref args, 18 + bytes.Length);
                    bytes.CopyTo(args, 18);
                }
                    break;
                case IkuraScript.Instruction.CNS:
                {
                    var bytes = _encoding.GetBytes(lines.Single());
                    Array.Resize(ref args, 2 + bytes.Length);
                    bytes.CopyTo(args, 2);
                }
                    break;
                case IkuraScript.Instruction.PM:
                case IkuraScript.Instruction.PMP:
                {
                    var messages = lines.Select(line => _encoding.GetBytes(line)).ToArray();
                    args = IkuraScript.Encode(args, 1, messages);
                }
                    break;
                case IkuraScript.Instruction.MSGBOX:
                {
                    var bytes = _encoding.GetBytes(lines.Single());
                    var end = args.Last();
                    Array.Resize(ref args, 4 + bytes.Length + 2);
                    bytes.CopyTo(args, 4);
                    args[4 + bytes.Length] = 0x00;
                    args[4 + bytes.Length + 1] = end;
                }
                    break;
                case IkuraScript.Instruction.MPM:
                    if (args[1] == 0) break;
                {
                    var messages = lines.Select(line => _encoding.GetBytes(line)).ToArray();
                    args = IkuraScript.Encode(args, 2, messages);
                }
                    break;
                case IkuraScript.Instruction.SETGAMEINFO:
                {
                    var bytes = _encoding.GetBytes(lines.Single());
                    Array.Resize(ref args, 0 + bytes.Length + 1);
                    bytes.CopyTo(args, 0);
                    args[0 + bytes.Length] = 0x00;
                }
                    break;
                default:
                    throw new NotSupportedException($"Import {instruction} is not supported");
            }

            return args;
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
    }
}