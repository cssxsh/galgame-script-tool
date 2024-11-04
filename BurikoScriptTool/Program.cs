using System;
using System.IO;
using System.Linq;
using System.Text;

namespace BGI
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var mode = "";
            var path = "*.arc";
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
                                .EnumerateFiles(".", path, SearchOption.TopDirectoryOnly)
                                .DefaultIfEmpty(path)
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

            switch (mode)
            {
                case "-e":
                    _encoding ??= Encoding.GetEncoding("SHIFT-JIS");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                    using (var stream = File.OpenRead(path))
                    using (var reader = new BinaryReader(stream))
                    {
                        // scripts = reader.ReadWillScripts();
                    }

                    Directory.CreateDirectory($"{path}~");

                    // foreach (var script in scripts)
                    // {
                    //     if (!script.HasText()) continue;
                    //     Console.WriteLine($"Export {script.Name}");
                    //     using var writer = File.CreateText($"{path}~/{script.Name}.txt");
                    //     for (var i = 0; i < script.Commands.Length; i++)
                    //     {
                    //         var text = Export(script.Commands[i]);
                    //         if (text == null) continue;
                    //         writer.WriteLine($">{script.Commands[i][1]:X2}");
                    //         writer.WriteLine($"◇{i:D4}◇{text}");
                    //         writer.WriteLine($"◆{i:D4}◆{text}");
                    //         writer.WriteLine();
                    //     }
                    // }

                    break;
                case "-i":
                    _encoding ??= Encoding.GetEncoding("GBK");
                    Console.WriteLine($"Read {Path.GetFullPath(path)}");
                    using (var stream = File.OpenRead(path))
                    using (var reader = new BinaryReader(stream))
                    {
                        // scripts = reader.ReadWillScripts();
                    }

                    // foreach (var script in scripts)
                    // {
                    //     if (!script.HasText()) continue;
                    //     if (!File.Exists($"{path}~/{script.Name}.txt")) continue;
                    //     Console.WriteLine($"Import {script.Name}");
                    //     var translated = new string[script.Commands.Length];
                    //     foreach (var line in File.ReadLines($"{path}~/{script.Name}.txt"))
                    //     {
                    //         var match = Regex.Match(line, @"◆(\d+)◆(.+)$");
                    //         if (!match.Success) continue;
                    //
                    //         var index = int.Parse(match.Groups[1].Value);
                    //         var text = match.Groups[2].Value;
                    //
                    //         translated[index] = text;
                    //     }
                    //
                    //     for (var i = 0; i < script.Commands.Length; i++)
                    //     {
                    //         if (translated[i] == null) continue;
                    //         script.Commands[i] = Import(script.Commands[i], translated[i]);
                    //     }
                    // }

                    // var filename =
                    //     $"{Path.GetFileNameWithoutExtension(path)}_{_encoding.WebName}{Path.GetExtension(path)}";
                    // Console.WriteLine($"Write {filename}");
                    // using (var stream = File.Create(filename))
                    // using (var writer = new BinaryWriter(stream))
                    // {
                    //     writer.WriteWillScripts(scripts);
                    // }

                    break;
                default:
                    // Array.Resize(ref scripts, 0);
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  Export text : BurikoScriptTool -e [*.arc] [encoding]");
                    Console.WriteLine("  Import text : BurikoScriptTool -i [*.arc] [encoding]");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
            }
        }

        private static Encoding _encoding = Encoding.Default;
    }
}