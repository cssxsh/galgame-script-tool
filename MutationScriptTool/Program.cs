using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ATool;

namespace Mutation
{
    internal static class Program
    {
        public static void Main(params string[] args)
        {
            var mode = "";
            var path = "scr.dpf";

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
                    var scripts = reader.ReadMutationScripts();

                    Directory.CreateDirectory($"{path}~");

                    foreach (var script in scripts)
                    {
                        if (!script.Value.HasText()) continue;
                        Console.WriteLine($"Export {script.Key}");
                        using var writer = File.CreateText($"{path}~/{script.Key}.txt");
                        var lines = script.Value.Split('\n');
                        var func = "";
                        for (var i = 0; i < lines.Length; i++)
                        {
                            var f = Regex.Match(lines[i], @"\w+?(?=\()");
                            if (f.Success) func = f.Value;
                            switch (func)
                            {
                                case "message":
                                case "monologue":
                                    break;
                                default:
                                    continue;
                            }

                            var s = Regex.Match(lines[i], @"(?<="").+(?="")");
                            if (!s.Success) continue;
                            writer.WriteLine($">{func}");
                            writer.WriteLine($"◇{i:D4}◇{s.Value}");
                            writer.WriteLine($"◆{i:D4}◆{s.Value}");
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
                    var scripts = reader.ReadMutationScripts();

                    for (var i = 0; i < scripts.Length; i++)
                    {
                        if (!scripts[i].Value.HasText()) continue;
                        if (!File.Exists($"{path}~/{scripts[i].Key}.txt")) continue;
                        Console.WriteLine($"Import {scripts[i].Key}");
                        var lines = scripts[i].Value.Split('\n');
                        foreach (var line in File.ReadLines($"{path}~/{scripts[i].Key}.txt"))
                        {
                            var match = Regex.Match(line, @"◆(\d+)◆(.+)$");
                            if (!match.Success) continue;

                            var index = int.Parse(match.Groups[1].Value);
                            var text = match.Groups[2].Value;

                            lines[index] = Regex.Replace(lines[index], @"(?<="").+(?="")", text);
                        }

                        scripts[i] = new KeyValuePair<string, string>(scripts[i].Key, string.Join("\n", lines));
                    }

                    var filename = path.PatchFileName(_encoding.WebName);
                    Console.WriteLine($"Write {filename}");
                    using var writer = new BinaryWriter(File.Create(filename));
                    writer.WriteMutationScripts(scripts);
                }
                    break;
                default:
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  Export text : MutationScriptTool -e [scr.dpf] [encoding]");
                    Console.WriteLine("  Import text : MutationScriptTool -i [scr.dpf] [encoding]");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
            }
        }

        private static Encoding _encoding = Encoding.Default;

        private const string FileHead = "DPFL";

        private static KeyValuePair<string, string>[] ReadMutationScripts(this BinaryReader reader)
        {
            var head = Encoding.ASCII.GetString(reader.ReadBytes(0x04));
            if (head != FileHead) throw new NotSupportedException($"unsupported version: {head}.");
            var count = reader.ReadUInt16();
            var scripts = new KeyValuePair<string, string>[count];

            for (var i = 0x00; i < count; i++)
            {
                reader.BaseStream.Position = 0x0000_0006 + i * 0x18;
                var name = Encoding.GetEncoding(932).GetString(reader.ReadBytes(0x10)).TrimEnd('\0');
                var offset = reader.ReadUInt32();
                var size = reader.ReadInt32();

                reader.BaseStream.Position = offset;
                var bytes = reader.ReadBytes(size);

                scripts[i] = new KeyValuePair<string, string>(name, Encoding.GetEncoding(932).GetString(bytes));
            }

            return scripts;
        }

        private static void WriteMutationScripts(this BinaryWriter writer, KeyValuePair<string, string>[] scripts)
        {
            writer.Write(Encoding.ASCII.GetBytes(FileHead));
            writer.Write((ushort)scripts.Length);

            var offset = 0x0000_0006 + scripts.Length * 0x18;
            for (var i = 0x00; i < scripts.Length; i++)
            {
                var text = scripts[i].Value;
                if (_encoding.CodePage == 936)
                {
                    text = text
                        .ReplaceHalfWidthKana()
                        .ReplaceGbkUnsupported();
                }

                var bytes = _encoding.GetBytes(text);

                writer.BaseStream.Position = 0x0000_0006 + i * 0x18;
                writer.Write(Encoding.GetEncoding(932).GetBytes(scripts[i].Key));
                writer.BaseStream.Position = 0x0000_0006 + i * 0x18 + 0x10;
                writer.Write(offset);
                writer.Write(bytes.Length);

                writer.BaseStream.Position = offset;
                writer.Write(bytes);
                offset += bytes.Length;
            }
        }

        private static bool HasText(this string script)
        {
            return script.Contains("message") || script.Contains("monologue");
        }
    }
}