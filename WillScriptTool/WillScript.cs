using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ATool;

namespace Will
{
    public readonly struct WillScript
    {
        public readonly string Name;

        public readonly byte[][] Commands;

        public WillScript(string name, byte[] bytes)
        {
            Name = name;

            var commands = new List<byte[]>();
            // ReSharper disable once RedundantAssignment
            var temp = Array.Empty<byte>();
            using (var steam = new MemoryStream(bytes))
            using (var reader = new BinaryReader(steam))
            {
                while (steam.Position < bytes.Length)
                {
                    var position = steam.Position;
                    var size = reader.ReadByte();
                    if (size == 0x00 || size == 0xFF)
                    {
                        steam.Position = position;
                        temp = reader.ReadBytes((int)(bytes.Length - position));
                        if (temp.Any(b => b != 0x00) && temp.Any(b => b != 0xFF))
                            throw new FormatException($"{name} at 0x{position:X8}");
                        commands.Add(temp);
                        break;
                    }
                    var instruction = reader.ReadByte();

                    switch (instruction)
                    {
                        case 0x01:
                        case 0x02:
                        case 0x03:
                        case 0x04:
                        case 0x05:
                        case 0x06:
                        case 0x07:
                        case 0x08:
                        case 0x09:
                        case 0x0A:
                        case 0x0B:
                        case 0x0C:
                        case 0x0D:
                        case 0x0E:
                        case 0x21:
                        case 0x26:
                        case 0x3E:
                        {
                            var offset = 2;
                            if (size >= 6 && bytes[position + 0x05] == 0x00)
                            {
                                var value = reader.ReadUInt32();
                                // Console.WriteLine($"0x{value:X8}");
                                offset += 4;
                            }
                            else if (bytes[position + 0x02] == 0x00)
                            {
                                var value = reader.ReadByte();
                                // Console.WriteLine($"0x{value:X2}");
                                offset += 1;
                            }

                            if (offset == size) break;
                            temp = reader.ReadBytes(size - offset).TrimEnd();
                            if (temp.Length != 0 && temp.Length != size - offset - 1)
                                throw new FormatException($"{name} at 0x{position:X8}");
                            // Console.WriteLine($"'{encoding.GetString(temp).ReplaceGbkUnsupported()}'");
                        }
                            break;
                        case 0x25:
                        {
                            temp = reader.ReadBytes(6);
                            // Console.WriteLine($"'{encoding.GetString(temp.TrimEnd())}'");
                        }
                            break;
                        case 0x47:
                        {
                            temp = reader.ReadBytes(6);
                            // Console.WriteLine(BitConverter.ToString(temp));
                            temp = reader.ReadBytes(size - 2 - 6);
                            // Console.WriteLine($"'{encoding.GetString(temp)}'");
                        }
                            break;
                        case 0x57:
                        {
                            if (size == 4)
                            {
                                temp = reader.ReadBytes(2);
                                // Console.WriteLine(BitConverter.ToString(temp));
                                break;
                            }
                            temp = reader.ReadBytes(4);
                            // Console.WriteLine(BitConverter.ToString(temp));
                            temp = reader.ReadBytes(size - 2 - 4).TrimEnd();
                            if (temp.Length != size - 2 - 4 - 1)
                                throw new FormatException($"{name} at 0x{position:X8}");
                            // Console.WriteLine($"'{encoding.GetString(temp)}'");
                        }
                            break;
                        case 0x23:
                        case 0x24:
                        case 0x2A:
                        case 0x2E:
                        case 0x3A:
                        case 0x40:
                        case 0x43:
                        case 0x46:
                        case 0x4D:
                        case 0x52:
                        case 0x7E:
                        {
                            temp = reader.ReadBytes(size - 2);
                            // Console.WriteLine(BitConverter.ToString(temp));
                        }
                            break;
                        default:
                        {
                            temp = reader.ReadBytes(size - 2);
                            // Console.WriteLine(BitConverter.ToString(temp));
                        }
                            throw new FormatException($"{name} with 0x{instruction:X2}");
                    }

                    steam.Position = position;
                    commands.Add(reader.ReadBytes(size));
                }
            }

            Commands = commands.ToArray();
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[Commands.Sum(command => command.Length)];
            var index = 0;
            foreach (var command in Commands)
            {
                command.CopyTo(bytes, index);
                index += command.Length;
            }
            
            return bytes;
        }
    }
}