using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATool;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace CatSystem
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var mode = "";
            var path = "scene.int";
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
                    Environment.SetEnvironmentVariable("GAME_PATH", Path.GetDirectoryName(path));
                    using var reader = new BinaryReader(File.OpenRead(path), Encoding.ASCII, true);
                    var scripts = reader.ReadKIF();

                    Directory.CreateDirectory($"{path}~");

                    foreach (var script in scripts)
                    {
                        switch (Path.GetExtension(script.Key))
                        {
                            case ".kcs":
                                Console.WriteLine($"Export {script.Key}");
                            {
                                var kcs = new KcScript(script.Key, script.Value);

                                using var writer = File.CreateText($"{path}~/{kcs.Name}.txt");
                                for (var i = 0x00; i < kcs.Texts.Length; i++)
                                {
                                    foreach (var line in _encoding
                                                 .GetString(kcs.Texts[i].Value)
                                                 .Split('\n'))
                                    {
                                        writer.WriteLine($">{kcs.Texts[i].Key:X8}");
                                        writer.WriteLine($"◇{i:D4}◇{line}");
                                        writer.WriteLine($"◆{i:D4}◆{line}");
                                        writer.WriteLine();
                                    }
                                }
                            }
                                break;
                            case ".fes":
                                Console.WriteLine($"Export {script.Key}");
                            {
                                var frame = new FrameScript(script.Key, script.Value);
                                var text = _encoding.GetString(frame.Content);

                                var name = Path.GetFileNameWithoutExtension(frame.Name);
                                File.WriteAllText($"{path}~/{name}.txt", text);
                            }
                                break;
                            case ".cst":
                                Console.WriteLine($"Export {script.Key}");
                            {
                                var scene = new SceneScript(script.Key, script.Value);
                                if (!scene.HasText()) continue;

                                using var writer = File.CreateText($"{path}~/{scene.Name}.txt");
                                for (var i = 0x00; i < scene.Commands.Length; i++)
                                {
                                    foreach (var line in Export(scene.Commands[i]))
                                    {
                                        writer.WriteLine($">{scene.Commands[i].Key:X4}");
                                        writer.WriteLine($"◇{i:D4}◇{line}");
                                        writer.WriteLine($"◆{i:D4}◆{line}");
                                        writer.WriteLine();
                                    }
                                }
                            }
                                break;
                            default:
                                continue;
                        }
                    }
                }
                    break;
                case "-i":
                    _encoding ??= Encoding.GetEncoding("GBK");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                {
                    Environment.SetEnvironmentVariable("GAME_PATH", Path.GetDirectoryName(path));
                    using var stream = File.OpenRead(path);
                    using var reader = new BinaryReader(stream);
                    var scripts = reader.ReadKIF();

                    var directory = Path
                        .Combine(Path.GetDirectoryName(path) ?? ".", Path.GetFileNameWithoutExtension(path))
                        .PatchFileName(_encoding.WebName);
                    Directory.CreateDirectory(directory);

                    foreach (var script in scripts)
                    {
                        switch (Path.GetExtension(script.Key))
                        {
                            case ".kcs":
                                Console.WriteLine($"Import {script.Key}");
                            {
                                var kcs = new KcScript(script.Key, script.Value);

                                var translated = new string[kcs.Texts.Length][];
                                foreach (var line in File.ReadLines($"{path}~/{kcs.Name}.txt"))
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

                                for (var i = 0; i < kcs.Texts.Length; i++)
                                {
                                    if (translated[i] == null) continue;
                                    var key = kcs.Texts[i].Key;
                                    var value = _encoding.GetBytes(string.Join("\n", translated[i]));
                                    kcs.Texts[i] = new KeyValuePair<uint, byte[]>(key, value);
                                }

                                var file = Path.Combine(directory, kcs.Name);
                                File.WriteAllBytes(file, kcs.ToBytes());
                            }
                                break;
                            case ".fes":
                                Console.WriteLine($"Import {script.Key}");
                            {
                                var name = Path.GetFileNameWithoutExtension(script.Key);
                                var text = File.ReadAllText($"{path}~/{name}.txt");
                                var frame = new FrameScript(script.Key, _encoding.GetBytes(text));

                                var file = Path.Combine(directory, frame.Name);
                                File.WriteAllBytes(file, frame.ToBytes());
                            }
                                break;
                            case ".cst":
                                Console.WriteLine($"Import {script.Key}");
                            {
                                var scene = new SceneScript(script.Key, script.Value);
                                if (!scene.HasText()) continue;

                                var translated = new string[scene.Commands.Length][];
                                foreach (var line in File.ReadLines($"{path}~/{scene.Name}.txt"))
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

                                for (var i = 0; i < scene.Commands.Length; i++)
                                {
                                    if (translated[i] == null) continue;
                                    scene.Commands[i] = Import(scene.Commands[i], translated[i]);
                                }

                                var file = Path.Combine(directory, scene.Name);
                                File.WriteAllBytes(file, scene.ToBytes());
                            }
                                break;
                            default:
                                continue;
                        }
                    }
                }
                    break;
                default:
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  Export text : CatSystemScriptTool -e [*.int] [encoding]");
                    Console.WriteLine("  Import text : CatSystemScriptTool -i [*.int] [encoding]");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
            }
        }

        private static Encoding _encoding = Encoding.Default;

        // ReSharper disable once InconsistentNaming
        private static KeyValuePair<string, byte[]>[] ReadKIF(this BinaryReader reader)
        {
            var head = Encoding.ASCII.GetString(reader.ReadBytes(0x04));
            if (head != "KIF\0") throw new NotSupportedException($"unsupported version: {head}.");
            var count = reader.ReadInt32();
            var offset = 0x0000_0008;
            var flag = Encoding.ASCII.GetString(reader.ReadBytes(0x40)).TrimEnd('\0');

            var blowfish = new BlowfishEngine();
            var twister = new MersenneTwister(0x0000_0000);
            var key = 0x0000_0000u;
            if (flag == "__key__.dat")
            {
                count--;
                offset += 0x48;
                var password = GetPassword("V_CODE2");
                Console.WriteLine($"Password: {password}");
                var code = Encoding.ASCII.GetBytes(password);
                var crc32 = code.Crc32();
                Debug.WriteLine($"CRC32: {crc32:X8}");
                var table = Hash.CrcTable(0x04C1_1DB7, 0x20);
                key = code.Aggregate(0xFFFF_FFFFu, (v, c) => ~table[(v >> 0x18) ^ c] ^ (v << 0x08));
                Debug.WriteLine($"KEY: {key:X8}");
                _ = reader.ReadUInt32();
                var seed = reader.ReadUInt32();
                twister.SRand(seed);

                var r = BitConverter.GetBytes(twister.Rand());
                blowfish.Init(false, new KeyParameter(r));
            }

            var scripts = new KeyValuePair<string, byte[]>[count];
            for (var i = 0x00; i < count; i++)
            {
                reader.BaseStream.Position = offset + i * 0x48;
                var temp = reader.ReadBytes(0x40);
                var pos = reader.ReadUInt32();
                var size = reader.ReadInt32();

                if (flag == "__key__.dat")
                {
                    // handle name
                    {
                        twister.SRand((uint)(key + i + 0x01));
                        var r = BitConverter.GetBytes(twister.Rand());
                        var k = (r[0x00] + r[0x01] + r[0x02] + r[0x03]) & 0xFF;
                        const string alphabet = "zyxwvutsrqponmlkjihgfedcbaZYXWVUTSRQPONMLKJIHGFEDCBA";
                        for (var j = 0; j < temp.Length; j++, k++)
                        {
                            if (temp[j] == 0x00) break;
                            var t = alphabet.IndexOf((char)temp[j]);
                            if (t == -1) continue;
                            t = (51 - t + k) % 52;
                            temp[j] = (byte)alphabet[t];
                        }
                    }
                    // handle offset and size
                    {
                        var block = new byte[0x08];
                        BitConverter.GetBytes(pos + i + 0x01).CopyTo(block, 0x00);
                        BitConverter.GetBytes(size).CopyTo(block, 0x04);
                        _ = blowfish.Process(block);
                        pos = BitConverter.ToUInt32(block, 0x00);
                        size = BitConverter.ToInt32(block, 0x04);
                    }
                }

                var name = Encoding.GetEncoding(932).GetString(temp).TrimEnd('\0');
                reader.BaseStream.Position = pos;
                var bytes = reader.ReadBytes(size);
                _ = blowfish.Process(bytes);

                scripts[i] = new KeyValuePair<string, byte[]>(name, bytes);
            }

            return scripts;
        }

        private static string[] Export(KeyValuePair<ushort, byte[]> command)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (command.Key)
            {
                case 0x0201:
                case 0x0301:
                    return Array.Empty<string>();
                case 0x2001:
                case 0x2101:
                {
                    return _encoding
                        .GetString(command.Value)
                        .TrimEnd('\0')
                        .Split('\n');
                }
                case 0x3001:
                    return Array.Empty<string>();
                default:
                    throw new FormatException($"unsupported command: {command.Key:X4}");
            }
        }

        private static KeyValuePair<ushort, byte[]> Import(KeyValuePair<ushort, byte[]> command, string[] lines)
        {
            var text = string.Join("\n", lines);
            if (_encoding.CodePage == 936) text = text.ReplaceGbkUnsupported();

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (command.Key)
            {
                case 0x0201:
                case 0x0301:
                    break;
                case 0x2001:
                case 0x2101:
                {
                    var instruction = command.Key;
                    var bytes = _encoding.GetBytes(text);
                    Array.Resize(ref bytes, bytes.Length + 0x01);
                    return new KeyValuePair<ushort, byte[]>(instruction, bytes);
                }
                case 0x3001:
                    break;
                default:
                    throw new FormatException($"unsupported command: {command.Key:X4}");
            }

            return command;
        }

        private static bool HasText(this SceneScript script)
        {
            return script.Commands
                .Any(command => command.Key switch
                {
                    0x0201 => false,
                    0x0301 => false,
                    0x2001 => true,
                    0x2101 => true,
                    0x3001 => false,
                    _ => throw new FormatException($"unsupported command: {command.Key:X4}")
                });
        }

        private static string GetPassword(string type)
        {
            var path = Environment.GetEnvironmentVariable("GAME_PATH") ?? ".";
            var blowfish = new BlowfishEngine();
            foreach (var exe in Directory.GetFiles(path, "*.exe"))
            {
                Console.WriteLine($"try read password from {Path.GetFullPath(exe)}");
                var types = exe.ReadResourceTypes();
                var key = Encoding.ASCII.GetBytes("windmill");
                if (types.Contains("KEY_CODE"))
                {
                    key = exe.ReadResource("KEY", "KEY_CODE");
                    key.Xor(0xCD);
                }

                Debug.WriteLine($"KEY#KEY_CODE: {Encoding.ASCII.GetString(key)}");

                if (!types.Contains(type)) continue;
                var code = exe.ReadResource("DATA", type);

                Debug.WriteLine($"DATA#{type}: {BitConverter.ToString(code)}");
                blowfish.Init(false, new KeyParameter(key));
                _ = blowfish.Process(code);

                return Encoding.ASCII.GetString(code).TrimEnd('\0');
            }

            throw new FormatException($"Not found password by {type}.");
        }

        private static int Process(this BlowfishEngine blowfish, byte[] data)
        {
            data.EndianReverse();
            var capacity = blowfish.GetBlockSize();
            var count = 0;

            for (var offset = 0; offset + capacity <= data.Length; offset += capacity)
            {
                count += blowfish.ProcessBlock(data, offset, data, offset);
            }

            data.EndianReverse();
            return count;
        }
    }
}