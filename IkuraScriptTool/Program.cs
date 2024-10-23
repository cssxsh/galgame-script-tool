﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IkuraScriptTool
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var mode = "";
            var path = "ISF";
            switch (args.Length)
            {
                case 1:
                    mode = args[0];
                    _encoding = mode == "-e"
                        ? Encoding.GetEncoding("SHIFT-JIS")
                        : Encoding.GetEncoding("GBK");
                    break;
                case 2:
                    mode = args[0];
                    path = args[1];
                    _encoding = mode == "-e"
                        ? Encoding.GetEncoding("SHIFT-JIS")
                        : Encoding.GetEncoding("GBK");
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
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                    using (var stream = File.OpenRead(path))
                    using (var reader = new BinaryReader(stream))
                    {
                        scripts = reader.ReadIkuraScripts();
                    }
                    Directory.CreateDirectory($"{path}~");
                    
                    foreach (var script in scripts)
                    {
                        Console.WriteLine($"Export {script.Name}");
                        using (var writer = File.CreateText($"{path}~/{script.Name}.txt"))
                        {
                            for (var i = 0; i < script.Commands.Length; i++)
                            {
                                foreach (var line in 
                                         Export(script.Commands[i].Key, script.Commands[i].Value))
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
                case "-p":
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                    using (var stream = File.OpenRead(path))
                    using (var reader = new BinaryReader(stream))
                    {
                        scripts = reader.ReadIkuraScripts();
                    }
                    
                    foreach (var script in scripts)
                    {
                        if (!File.Exists($"{path}~/{script.Name}.txt")) continue;
                        Console.WriteLine($"Patch {script.Name}");
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
                        }
                        
                        for (var i = 0; i < script.Commands.Length; i++)
                        {
                            if (translated[i] == null) continue;
                            var instruction = script.Commands[i].Key;
                            var bytes = script.Commands[i].Value;
                            Patch(instruction, ref bytes, translated[i]);
                            script.Commands[i] = new KeyValuePair<IkuraScript.Instruction,byte[]>(instruction, bytes);
                        }
                    }
                    
                    Console.WriteLine($"Write {path}_{_encoding.WebName}");
                    using (var stream = File.Create($"{path}_{_encoding.WebName}"))
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.WriteIkuraScripts(scripts);
                    }

                    break;
                default:
                    Array.Resize(ref scripts, 0);
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  Export text  : IkuraScriptTool -e [ISF] [encoding]");
                    Console.WriteLine("  Patch script : IkuraScriptTool -p [ISF] [encoding]");
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
                var name = _encoding.GetString(reader.ReadBytes(12).TrimEnd());
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
            offset = (offset + 0x0F) & 0xFFFF_FFF0;
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
                offset += (uint)(bytes.Length + 0x0F) & 0xFFFF_FFF0u;
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
                    return new [] { _encoding.GetString(args, 18, args.Length - 18) };
                case IkuraScript.Instruction.CNS:
                    return new [] { _encoding.GetString(args, 2, args.Length - 2) };
                case IkuraScript.Instruction.PM:
                case IkuraScript.Instruction.PMP:
                    return IkuraScript.Decode(args, 1)
                        .Select(line => _encoding.GetString(line))
                        .ToArray();
                case IkuraScript.Instruction.MSGBOX:
                    return new [] { _encoding.GetString(args, 4, args.Length - 6) };
                case IkuraScript.Instruction.MPM:
                    if (args[1] == 0) return Array.Empty<string>();
                    return IkuraScript.Decode(args, 2)
                        .Select(line => _encoding.GetString(line))
                        .ToArray();
                case IkuraScript.Instruction.SETGAMEINFO:
                    return new [] { _encoding.GetString(args, 0, args.Length - 1) };
                default:
                    return Array.Empty<string>();
            }
        }
        
        private static void Patch(IkuraScript.Instruction instruction, ref byte[] args, string[] lines)
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
                    IkuraScript.Encode(ref args, 1, messages);
                }
                    break;
                case IkuraScript.Instruction.MSGBOX:
                {
                    var bytes = _encoding.GetBytes(lines.Single());
                    Array.Resize(ref args, 4 + bytes.Length + 1);
                    bytes.CopyTo(args, 4);
                }
                    break;
                case IkuraScript.Instruction.MPM:
                    if (args[1] == 0) break;
                {
                    var messages = lines.Select(line => _encoding.GetBytes(line)).ToArray();
                    IkuraScript.Encode(ref args, 2, messages);
                }
                    break;
                case IkuraScript.Instruction.SETGAMEINFO:
                {
                    var bytes = _encoding.GetBytes(lines.Single());
                    Array.Resize(ref args, 0 + bytes.Length + 1);
                    bytes.CopyTo(args, 0);
                }
                    break;
            }
        }

        private static byte[] TrimEnd(this byte[] source)
        {
            var target = (byte[])source.Clone();
            for (var i = 0; i < source.Length; i++)
            {
                if (source[i] != 0x00) continue;
                Array.Resize(ref target, i);
                break;
            }

            return target;
        }
    }
}