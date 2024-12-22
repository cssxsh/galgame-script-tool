using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using ATool;

namespace BGI
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public readonly struct BurikoProgramPatch
    {
        public readonly string Name;

        public readonly uint Header;

        public readonly uint Offset;

        public readonly KeyValuePair<uint, byte[]>[] Data;

        public BurikoProgramPatch(string name, byte[] source)
        {
            Name = name;

            using var stream = new MemoryStream(source);
            using var reader = new BinaryReader(stream);

            var data = new List<KeyValuePair<uint, byte[]>>();
            Header = reader.ReadUInt32();
            if (Header == 0x6972_7542u) // 'BurikoCompiledScript'
            {
                var cp932 = Encoding.GetEncoding(932);

                stream.Position = 0x0000_0000;
                var header = Encoding.ASCII.GetString(reader.ReadBytes(0x1C).TrimEnd());
                switch (header)
                {
                    case "BurikoCompiledScriptVer1.00":
                        break;
                    default:
                        throw new FormatException($"unsupported: {header}");
                }

                var command = 0x0000_001C + reader.ReadUInt32();
                var size = (uint)source.Length;
                var position = command;
                while (position < size)
                {
                    stream.Position = position;
                    var instruction = reader.ReadUInt32();
                    switch (instruction)
                    {
                        // PUSH Value
                        case 0x0000_0000:
                        case 0x0000_0001:
                        case 0x0000_0002:
                        case 0x0000_0008:
                        {
                            var value = reader.ReadUInt32();

                            Debug.WriteLine($"{position:X8}>{instruction:X2} PUSH {value:X8}");
                        }
                            position += 0x08;
                            break;
                        // PUSH Offset
                        case 0x0000_0003:
                        {
                            var diff = reader.ReadUInt32();

                            stream.Position = command + diff;
                            if (size > stream.Position) size = (uint)stream.Position;
                            var count = (int)(source.Length - position > 0x80 ? 0x80 : source.Length - position);
                            var bytes = reader.ReadBytes(count).TrimEnd();
                            data.Add(new KeyValuePair<uint, byte[]>(position, bytes));

                            Debug.WriteLine($"{position:X8}>{instruction:X2} PUSH {diff:X8}");
                            Debug.WriteLine($"'{cp932.GetString(bytes)}'");
                        }
                            position += 0x08;
                            break;
                        case 0x0000_0009:
                        case 0x0000_000A:
                        case 0x0000_003F:
                        {
                            var value = reader.ReadUInt32();

                            Debug.WriteLine($"{position:X8}>{instruction:X2} ???? {value:X8}");
                        }
                            position += 0x08;
                            break;
                        // CALL
                        case 0x0000_0010:
                        case 0x0000_0011:
                        case 0x0000_0018:
                        case 0x0000_001A:
                        case 0x0000_001B:
                        case 0x0000_001C:
                        case 0x0000_001D:
                        case 0x0000_001E:
                        case 0x0000_001F:
                        case 0x0000_0020:
                        case 0x0000_0021:
                        case 0x0000_0022:
                        case 0x0000_0023:
                        case 0x0000_0030:
                        case 0x0000_0031:
                        case 0x0000_0032:
                        case 0x0000_0033:
                        case 0x0000_0034:
                        case 0x0000_0035:
                        case 0x0000_0038:
                        case 0x0000_0039:
                        case 0x0000_0090:
                        case 0x0000_0091:
                        case 0x0000_0092:
                        case 0x0000_0093:
                        case 0x0000_0094:
                        case 0x0000_0095:
                        case 0x0000_00E0:
                        case 0x0000_00E1:
                        case 0x0000_00E2:
                        case 0x0000_00E3:
                        case 0x0000_00E4:
                        case 0x0000_00E5:
                        case 0x0000_00E6:
                        case 0x0000_00F3:
                        case 0x0000_00F4:
                        case 0x0000_00F6:
                        case 0x0000_00F7:
                        case 0x0000_00F9:
                        case 0x0000_0103:
                        case 0x0000_0108:
                        case 0x0000_0110:
                        case 0x0000_0111:
                        case 0x0000_0118:
                        case 0x0000_011A:
                        case 0x0000_0122:
                        case 0x0000_0123:
                        case 0x0000_0129:
                        case 0x0000_012A:
                        case 0x0000_0139:
                        case 0x0000_013A:
                        case 0x0000_013B:
                        case 0x0000_013E:
                        case 0x0000_0140:
                        case 0x0000_0141:
                        case 0x0000_0142:
                        case 0x0000_0143:
                        case 0x0000_0145:
                        case 0x0000_0148:
                        case 0x0000_0149:
                        case 0x0000_014B:
                        case 0x0000_0150:
                        case 0x0000_0151:
                        case 0x0000_0152:
                        case 0x0000_0153:
                        case 0x0000_0154:
                        case 0x0000_0155:
                        case 0x0000_0156:
                        case 0x0000_015A:
                        case 0x0000_015D:
                        case 0x0000_015F:
                        case 0x0000_016A:
                        case 0x0000_016B:
                        case 0x0000_016E:
                        case 0x0000_0170:
                        case 0x0000_0180:
                        case 0x0000_0181:
                        case 0x0000_0184:
                        case 0x0000_0185:
                        case 0x0000_0190:
                        case 0x0000_0191:
                        case 0x0000_0192:
                        case 0x0000_0193:
                        case 0x0000_0194:
                        case 0x0000_01A0:
                        case 0x0000_01A1:
                        case 0x0000_01A2:
                        case 0x0000_01A3:
                        case 0x0000_01A4:
                        case 0x0000_01A8:
                        case 0x0000_01A9:
                        case 0x0000_01AA:
                        case 0x0000_01AB:
                        case 0x0000_01AC:
                        case 0x0000_01BF:
                        case 0x0000_01D0:
                        case 0x0000_01E0:
                        case 0x0000_01D8:
                        case 0x0000_01D9:
                        case 0x0000_01F8:
                        case 0x0000_0230:
                        case 0x0000_0231:
                        case 0x0000_0234:
                        case 0x0000_0235:
                        case 0x0000_0236:
                        case 0x0000_0237:
                        case 0x0000_0240:
                        case 0x0000_0241:
                        case 0x0000_0242:
                        case 0x0000_0243:
                        case 0x0000_0244:
                        case 0x0000_025E:
                        case 0x0000_025F:
                        case 0x0000_027F:
                        case 0x0000_0280:
                        case 0x0000_0281:
                        case 0x0000_0282:
                        case 0x0000_0283:
                        case 0x0000_0284:
                        case 0x0000_0285:
                        case 0x0000_0286:
                        case 0x0000_0287:
                        case 0x0000_0288:
                        case 0x0000_02A0:
                        case 0x0000_02A2:
                        case 0x0000_02A8:
                        case 0x0000_02C0:
                        case 0x0000_02C1:
                        case 0x0000_02C2:
                        case 0x0000_02C3:
                        case 0x0000_02C4:
                        case 0x0000_02C5:
                        case 0x0000_02C6:
                        case 0x0000_02C7:
                        case 0x0000_02C8:
                        case 0x0000_02DF:
                        case 0x0000_02E0:
                        case 0x0000_02E1:
                        case 0x0000_02E2:
                        case 0x0000_02E3:
                        case 0x0000_02E4:
                        case 0x0000_02E5:
                        case 0x0000_02E6:
                        case 0x0000_02E7:
                        case 0x0000_0300:
                        case 0x0000_0301:
                        case 0x0000_0306:
                        case 0x0000_031E:
                        case 0x0000_031F:
                        case 0x0000_033F:
                        case 0x0000_0340:
                        case 0x0000_0348:
                        case 0x0000_0350:
                        case 0x0000_0380:
                        case 0x0000_0388:
                        case 0x0000_0393:
                        case 0x0000_03F1:
                        {
                            Debug.WriteLine($"{position:X8}>{instruction:X4} CALL");
                        }
                            position += 0x04;
                            break;
                        case 0x0000_0019:
                        {
                            var value = reader.ReadUInt32();
                            Debug.WriteLine($"{position:X8}>{instruction:X4} CALL {value:X8}");
                        }
                            position += 0x08;
                            break;
                        // LABEL
                        case 0x0000_007F:
                        {
                            var offset = reader.ReadUInt32();
                            var line = reader.ReadUInt32();

                            stream.Position = command + offset;
                            if (size > stream.Position) size = (uint)stream.Position;
                            var count = (int)(source.Length - offset > 0x80 ? 0x80 : source.Length - offset);
                            var bytes = reader.ReadBytes(count).TrimEnd();

                            Debug.WriteLine($"{position:X8}>{instruction:X2} LABEL");
                            Debug.WriteLine($"{cp932.GetString(bytes)}:{line:D4}");
                        }
                            position += 0x0C;
                            break;
                        default:
                            throw new FormatException($"ERROR: {Name} at {position:X8} with {instruction:X2}");
                    }
                }

                Offset = size;
            }
            else if (Name.EndsWith("._bp")) // ._bp
            {
                var position = Header;
                var size = reader.ReadUInt32();
                stream.Position = position;
                while (stream.Position < size)
                {
                    position = (uint)stream.Position;
                    var instruction = reader.ReadByte();
                    var capacity = instruction switch
                    {
                        0x00 => 0x01,
                        0x01 => 0x02,
                        0x02 => 0x04,
                        0x04 => 0x02,
                        0x05 => 0x02,
                        0x06 => 0x02,
                        0x08 => 0x01,
                        0x09 => 0x01,
                        0x0A => 0x01,
                        0x0C => 0x02,
                        0x10 => 0x00,
                        0x11 => 0x00,
                        0x14 => 0x00,
                        0x15 => 0x01,
                        0x16 => 0x00,
                        0x17 => 0x00,
                        0x20 => 0x00,
                        0x21 => 0x00,
                        0x22 => 0x00,
                        0x23 => 0x00,
                        0x24 => 0x00,
                        0x25 => 0x00,
                        0x26 => 0x00,
                        0x27 => 0x00,
                        0x28 => 0x00,
                        0x29 => 0x00,
                        0x2A => 0x00,
                        0x2B => 0x00,
                        0x30 => 0x00,
                        0x31 => 0x00,
                        0x32 => 0x00,
                        0x33 => 0x00,
                        0x34 => 0x00,
                        0x35 => 0x00,
                        0x38 => 0x00,
                        0x39 => 0x00,
                        0x3A => 0x00,
                        0x40 => 0x00,
                        0x41 => 0x00,
                        0x42 => 0x00,
                        0x43 => 0x00,
                        0x44 => 0x00,
                        0x45 => 0x00,
                        0x48 => 0x00,
                        0x49 => 0x00,
                        0x50 => 0x00,
                        0x51 => 0x00,
                        0x52 => 0x00,
                        0x53 => 0x00,
                        0x54 => 0x00,
                        0x55 => 0x00,
                        0x56 => 0x00,
                        0x57 => 0x00,
                        0x58 => 0x00,
                        0x59 => 0x00,
                        0x5A => 0x00,
                        0x5B => 0x00,
                        0x5D => 0x00,
                        0x5E => 0x00,
                        0x5F => 0x00,
                        0x60 => 0x00,
                        0x61 => 0x00,
                        0x62 => 0x00,
                        0x63 => 0x00,
                        0x64 => 0x00,
                        0x65 => 0x00,
                        0x66 => 0x00,
                        0x67 => 0x00,
                        0x68 => 0x00,
                        0x69 => 0x00,
                        0x6A => 0x00,
                        0x6B => 0x00,
                        0x6C => 0x00,
                        0x6D => 0x00,
                        0x6E => 0x00,
                        0x6F => 0x00,
                        0x70 => 0x00,
                        0x71 => 0x00,
                        0x74 => 0x00,
                        0x75 => 0x00,
                        0x77 => 0x00,
                        0x78 => 0x00,
                        0x79 => 0x00,
                        0x7A => 0x00,
                        0x7B => 0x00,
                        0x7C => 0x00,
                        0x7D => 0x00,
                        0x7E => 0x00,
                        0x7F => 0x00,
                        0x80 => 0x01,
                        0x81 => 0x01,
                        0x90 => 0x01,
                        0x91 => 0x01,
                        0x92 => 0x01,
                        0xA0 => 0x01,
                        0xB0 => 0x01,
                        0xC0 => 0x01,
                        0xE0 => 0x01,
                        0xFF => 0x01,
                        _ => throw new FormatException($"{Name} at {position:X8}: {instruction:X2}")
                    };

                    if (instruction == 0x05)
                    {
                        var offset = reader.ReadUInt16();
                        stream.Position = position + offset;
                        if (size > stream.Position) size = (uint)stream.Position;
                        var count = source.Length - offset > 0x80 ? 0x80 : source.Length - offset;
                        var bytes = reader.ReadBytes(count).TrimEnd();
                        data.Add(new KeyValuePair<uint, byte[]>(position, bytes));
                    }

                    stream.Position = position + 1 + capacity;
                }

                Offset = size;
            }
            else
            {
                Offset = (uint)source.Length;
            }

            Data = data.ToArray();
        }
    }
}