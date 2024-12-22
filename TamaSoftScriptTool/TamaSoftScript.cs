using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace TamaSoft
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public readonly struct TamaSoftScript
    {
        public readonly string Name;

        public readonly string Version;

        public readonly uint Key;

        public readonly uint X08;

        public readonly int[] Labels;

        public readonly byte[][] Commands;

        public TamaSoftScript(string name, byte[] bytes)
        {
            Name = name;

            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            Version = Encoding.ASCII.GetString(reader.ReadBytes(0x04));
            switch (Version)
            {
                case "SNR ":
                    break;
                case "SNR@":
                    break;
                default:
                    throw new FormatException($"unsupported version: {Version}");
            }

            Key = reader.ReadUInt32();
            X08 = reader.ReadUInt32();

            Labels = new int[reader.ReadUInt32()];
            var table = new uint[Labels.Length];
            for (var i = 0x00; i < Labels.Length; i++) table[i] = reader.ReadUInt32();
            var offset = 0x0000_00010 + Labels.Length * 0x04;

            var commands = new List<byte[]>();
            while (stream.Position < bytes.Length)
            {
                var position = stream.Position;

                for (var j = 0x00; j < table.Length; j++)
                {
                    if (table[j] != stream.Position - offset) continue;
                    Labels[j] = commands.Count;
                }

                var instruction = reader.ReadUInt32();
                var size = 0x04;

                switch (instruction)
                {
                    // @BEGIN
                    case 0x0000_0101:
                    {
                        size += 0x04;
                        var len = reader.ReadInt32();
                        size += len;
                        var raw = reader.ReadBytes(len);
                        if (!Debugger.IsAttached) break;
                        TamaSoftSecret.Handle(raw, Key);
                        var text = Encoding.GetEncoding(932).GetString(raw);
                        Debug.WriteLine($"{Name}:{position:X8}>BEGIN '{text}'");
                    }
                        break;
                    // @END
                    case 0x0000_0202:
                    {
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>END");
                    }
                        break;
                    // @SHAKE(@UPDATE)
                    case 0x0000_0233:
                    {
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>SHAKE");
                    }
                        break;
                    // @FLUSH(@UPDATE)
                    case 0x0000_0234:
                    {
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>FLUSH");
                    }
                        break;
                    // @SOUND_STOP
                    case 0x0000_0249:
                    {
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>SOUND_STOP");
                    }
                        break;
                    // @RAIN_STOP
                    case 0x0000_02B1:
                    {
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>RAIN_STOP");
                    }
                        break;
                    // @SNOW_STOP
                    case 0x0000_02B3:
                    {
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>SNOW_STOP");
                    }
                        break;
                    // @WIN_COL_RESET
                    case 0x0000_02F1:
                    {
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>WIN_COL_RESET");
                    }
                        break;
                    // @SKIP_ENABLE
                    case 0x0000_02FA:
                    {
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>SKIP_ENABLE");
                    }
                        break;
                    // @SKIP_DISABLE
                    case 0x0000_02FB:
                    {
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>SKIP_DISABLE");
                    }
                        break;
                    // @TEXT
                    case 0x0000_0303:
                    {
                        size += 0x04;
                        var index = reader.ReadInt32();
                        size += 0x04;
                        var a = reader.ReadBytes(reader.ReadInt32());
                        size += a.Length;
                        size += 0x04;
                        var b = reader.ReadBytes(reader.ReadInt32());
                        size += b.Length;
                        size += 0x04;
                        var c = reader.ReadBytes(reader.ReadInt32());
                        size += c.Length;
                        if (!Debugger.IsAttached) break;
                        TamaSoftSecret.Handle(a, Key);
                        var s1 = Encoding.GetEncoding(932).GetString(a);
                        TamaSoftSecret.Handle(b, Key);
                        var s2 = Encoding.GetEncoding(932).GetString(b);
                        TamaSoftSecret.Handle(c, Key);
                        var s3 = Encoding.GetEncoding(932).GetString(c);
                        Debug.WriteLine($"{Name}:{position:X8}>TEXT {index:X8}, '{s1}', '{s2}', '{s3}'");
                    }
                        break;
                    // @SELECT
                    case 0x0000_0605:
                    {
                        var v = new int[4];
                        var r = new byte[4][];
                        for (var i = 0; i < 0x04; i++)
                        {
                            size += 0x04;
                            v[i] = reader.ReadInt32();
                            size += v[i];
                            r[i] = reader.ReadBytes(v[i]);
                            size += 0x04;
                            v[i] = reader.ReadInt32();
                        }

                        if (!Debugger.IsAttached) break;
                        var s = new string[4];
                        for (var i = 0; i < 0x04; i++)
                        {
                            TamaSoftSecret.Handle(r[i], Key);
                            s[i] = Encoding.GetEncoding(932).GetString(r[i]);
                        }

                        Debug.Write($"{Name}:{position:X8}>SELECT " +
                                    $"'{s[0]}', {v[0]:X8}, " +
                                    $"'{s[1]}', {v[1]:X8}, " +
                                    $"'{s[2]}', {v[2]:X8}, " +
                                    $"'{s[3]}', {v[3]:X8}");
                    }
                        break;
                    // @GOTO
                    case 0x0000_0710:
                    {
                        size += 0x04;
                        var index = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>GOTO {index:X8}");
                    }
                        break;
                    // @EXECUTE
                    case 0x0000_0811:
                    {
                        size += 0x04;
                        var raw = reader.ReadBytes(reader.ReadInt32());
                        size += raw.Length;
                        if (!Debugger.IsAttached) break;
                        TamaSoftSecret.Handle(raw, Key);
                        var text = Encoding.GetEncoding(932).GetString(raw);
                        Debug.WriteLine($"{Name}:{position:X8}>EXECUTE '{text}'");
                    }
                        break;
                    // @SOUND
                    case 0x0000_0848:
                    {
                        size += 0x04;
                        var raw = reader.ReadBytes(reader.ReadInt32());
                        size += raw.Length;
                        if (!Debugger.IsAttached) break;
                        TamaSoftSecret.Handle(raw, Key);
                        var text = Encoding.GetEncoding(932).GetString(raw);
                        Debug.WriteLine($"{Name}:{position:X8}>SOUND '{text}'");
                    }
                        break;
                    // @ENVIRONMENT
                    case 0x0000_0944:
                    {
                        size += 0x04;
                        var raw = reader.ReadBytes(reader.ReadInt32());
                        size += raw.Length;
                        size += 0x04;
                        var a = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        TamaSoftSecret.Handle(raw, Key);
                        var text = Encoding.GetEncoding(932).GetString(raw);
                        Debug.WriteLine($"{Name}:{position:X8}>ENVIRONMENT '{text}', {a:X8}");
                    }
                        break;
                    // @SWF
                    case 0x0000_09D0:
                    {
                        size += 0x04;
                        var raw = reader.ReadBytes(reader.ReadInt32());
                        size += raw.Length;
                        size += 0x04;
                        var a = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        TamaSoftSecret.Handle(raw, Key);
                        var text = Encoding.GetEncoding(932).GetString(raw);
                        Debug.WriteLine($"{Name}:{position:X8}>SWF '{text}', {a:X8}");
                    }
                        break;
                    // @???
                    // case 0x0000_09D1:
                    //     break;
                    // @BG
                    case 0x0000_0A20:
                    {
                        size += 0x04;
                        var raw = reader.ReadBytes(reader.ReadInt32());
                        size += raw.Length;
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        TamaSoftSecret.Handle(raw, Key);
                        var text = Encoding.GetEncoding(932).GetString(raw);
                        Debug.WriteLine($"{Name}:{position:X8}>BG '{text}', {a:X8}, {b:X8}");
                    }
                        break;
                    // @CG
                    case 0x0000_0A21:
                    {
                        size += 0x04;
                        var raw = reader.ReadBytes(reader.ReadInt32());
                        size += raw.Length;
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        TamaSoftSecret.Handle(raw, Key);
                        var text = Encoding.GetEncoding(932).GetString(raw);
                        Debug.WriteLine($"{Name}:{position:X8}>CG '{text}', {a:X8}, {b:X8}");
                    }
                        break;
                    // @MOVIE
                    case 0x0000_0AC0:
                    {
                        size += 0x04;
                        var raw = reader.ReadBytes(reader.ReadInt32());
                        size += raw.Length;
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        TamaSoftSecret.Handle(raw, Key);
                        var text = Encoding.GetEncoding(932).GetString(raw);
                        Debug.WriteLine($"{Name}:{position:X8}>MOVIE '{text}', {a:X8}, {b:X8}");
                    }
                        break;
                    // @CHAR
                    case 0x0000_0B23:
                    {
                        size += 0x04;
                        var raw = reader.ReadBytes(reader.ReadInt32());
                        size += raw.Length;
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        size += 0x04;
                        var c = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        TamaSoftSecret.Handle(raw, Key);
                        var text = Encoding.GetEncoding(932).GetString(raw);
                        Debug.WriteLine($"{Name}:{position:X8}>CHAR '{text}', {a:X8}, {b:X8}, {c:X8}");
                    }
                        break;
                    // @FONT_SIZE
                    case 0x0000_2107:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>FONT_SIZE {a:X8}");
                    }
                        break;
                    // @FADEIN
                    case 0x0000_2135:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>FADEIN {a:X8}");
                    }
                        break;
                    // @PLAY_STOP
                    case 0x0000_2141:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>PLAY_STOP {a:X8}");
                    }
                        break;
                    // @ENVIRONMENT_STOP
                    case 0x0000_2145:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>ENVIRONMENT_STOP {a:X8}");
                    }
                        break;
                    // @WAIT
                    case 0x0000_2160:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>WAIT {a:X8}");
                    }
                        break;
                    // @CODE
                    case 0x0000_2180:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>CODE {a:X8}");
                    }
                        break;
                    // @RAIN
                    case 0x0000_21B0:
                    {
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>RAIN");
                    }
                        break;
                    // @SNOW
                    case 0x0000_21B2:
                    {
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>SNOW");
                    }
                        break;
                    // @BTL
                    case 0x0000_21E0:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>BTL {a:X8}");
                    }
                        break;
                    // @TEXT_OFF
                    case 0x0000_2206:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>TEXT_OFF {a:X8}, {b:X8}");
                    }
                        break;
                    // @UPDATE
                    case 0x0000_2231:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>UPDATE {a:X8}, {b:X8}");
                    }
                        break;
                    // @PLAY
                    case 0x0000_2240:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>PLAY {a:X8}, {b:X8}");
                    }
                        break;
                    // @PLAY_VOLUME
                    case 0x0000_2243:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>PLAY_VOLUME {a:X8}, {b:X8}");
                    }
                        break;
                    // @ENVIRONMENT_VOLUME
                    case 0x0000_2246:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>ENVIRONMENT_VOLUME {a:X8}, {b:X8}");
                    }
                        break;
                    // @LUSTER
                    case 0x0000_22A0:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>LUSTER {a:X8}, {b:X8}");
                    }
                        break;
                    // @CHAR_CLEAR
                    case 0x0000_2324:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        size += 0x04;
                        var c = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>CHAR_CLEAR {a:X8}, {b:X8}, {c:X8}");
                    }
                        break;
                    // @WIN_COL
                    case 0x0000_23F0:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        size += 0x04;
                        var c = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>WIN_COL {a:X8}, {b:X8}, {c:X8}");
                    }
                        break;
                    // @FADEOUT
                    case 0x0000_2430:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        size += 0x04;
                        var c = reader.ReadInt32();
                        size += 0x04;
                        var d = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>FADEOUT {a:X8}, {b:X8}, {c:X8}, {d:X8}");
                    }
                        break;
                    // @RAIN2
                    case 0x0000_24B4:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>RAIN2 {a:X8}, {b:X8}");
                    }
                        break;
                    // @SET
                    case 0x0000_3050:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>SET {a:X8}, {b:X8}");
                    }
                        break;
                    // @ADD
                    case 0x0000_3051:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>ADD {a:X8}, {b:X8}");
                    }
                        break;
                    // @DEC
                    case 0x0000_3052:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>DEC {a:X8}, {b:X8}");
                    }
                        break;
                    // @4070
                    case 0x0000_4070:
                    {
                        size += 0x04;
                        var a = reader.ReadInt32();
                        size += 0x04;
                        var b = reader.ReadInt32();
                        size += 0x04;
                        var c = reader.ReadInt32();
                        size += 0x04;
                        var d = reader.ReadInt32();
                        if (!Debugger.IsAttached) break;
                        Debug.WriteLine($"{Name}:{position:X8}>CALC {a:X8}, {b:X8}, {c:X8}, {d:X8}");
                    }
                        break;
                    // ...
                    default:
                        throw new FormatException($"{instruction:X8} at {Name}:{position:X8}");
                }

                stream.Position = position;
                commands.Add(reader.ReadBytes(size));
            }

            Commands = commands.ToArray();
        }

        public byte[] ToBytes()
        {
            var offset = (uint)(0x0000_00010 + Labels.Length * 0x04);
            var bytes = new byte[offset + Commands.Sum(command => command.Length)];

            using var stream = new MemoryStream(bytes);
            using var writer = new BinaryWriter(stream);

            writer.Write(Encoding.ASCII.GetBytes(Version));
            writer.Write(Key);
            writer.Write(X08);
            writer.Write(Labels.Length);

            stream.Position = offset;
            for (var i = 0x00; i < Commands.Length; i++)
            {
                var position = (uint)stream.Position;

                for (var j = 0x00; j < Labels.Length; j++)
                {
                    if (Labels[j] != i) continue;
                    stream.Position = 0x0000_00010 + j * 0x04;
                    writer.Write(position - offset);
                }

                stream.Position = position;
                writer.Write(Commands[i]);
            }

            return bytes;
        }
    }
}