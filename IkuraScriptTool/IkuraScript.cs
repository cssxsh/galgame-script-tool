using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Ikura
{
    public readonly struct IkuraScript
    {
        public readonly string Name;

        public readonly uint Offset;

        public readonly ushort Version;

        public readonly byte Key;

        public readonly byte Unused;

        public readonly int[] Labels;

        public readonly KeyValuePair<Instruction, byte[]>[] Commands;

        public IkuraScript(string name, byte[] bytes)
        {
            Name = name;

            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                Offset = reader.ReadUInt32();
                Version = reader.ReadUInt16();
                Key = reader.ReadByte();
                Unused = reader.ReadByte();
            }

            switch (Version)
            {
                case 0x9795:
                    for (var i = 8; i < bytes.Length; i++) bytes[i] = (byte)(bytes[i] >> 2 | bytes[i] << 6);
                    break;
                case 0xD197:
                    for (var i = 8; i < bytes.Length; i++) bytes[i] = (byte)~bytes[i];
                    break;
                case 0xCE89:
                    for (var i = 8; i < bytes.Length; i++) bytes[i] = (byte)(bytes[i] ^ Key);
                    break;
            }

            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                stream.Position = 8;
                Labels = new int[(Offset - 8) / 4];
                var table = new int[Labels.Length];
                for (var i = 0; i < table.Length; i++)
                {
                    table[i] = reader.ReadInt32();
                }

                var commands = new List<KeyValuePair<Instruction, byte[]>>();

                stream.Position = Offset;
                while (stream.Position < bytes.Length)
                {
                    for (var j = 0; j < table.Length; j++)
                    {
                        if (table[j] != stream.Position - Offset) continue;
                        Labels[j] = commands.Count;
                    }

                    var instruction = (Instruction)reader.ReadByte();
                    var size = (int)reader.ReadByte();
                    if (size > 0x7F)
                    {
                        size = ((size & 0x7F) << 8) | reader.ReadByte();
                        size -= 3;
                    }
                    else
                    {
                        size -= 2;
                    }

                    commands.Add(new KeyValuePair<Instruction, byte[]>(instruction, reader.ReadBytes(size)));
                }

                Commands = commands.ToArray();
            }
        }

        public byte[] ToBytes()
        {
            var size = Offset;
            foreach (var command in Commands)
            {
                size += (uint)(2 + command.Value.Length > 0x7F ? 3 : 2);
                size += (uint)command.Value.Length;
            }

            var bytes = new byte[size];
            using (var stream = new MemoryStream(bytes))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Offset);
                writer.Write(Version);
                writer.Write(Key);
                writer.Write(Unused);

                var position = Offset;
                for (var i = 0; i < Commands.Length; i++)
                {
                    for (var j = 0; j < Labels.Length; j++)
                    {
                        if (Labels[j] != i) continue;
                        stream.Position = 8 + j * 4;
                        writer.Write(position - Offset);
                    }

                    stream.Position = position;
                    writer.Write((byte)Commands[i].Key);
                    if (2 + Commands[i].Value.Length > 0x7F)
                    {
                        writer.Write((byte)((3 + Commands[i].Value.Length) >> 8 | 0x80));
                        writer.Write((byte)((3 + Commands[i].Value.Length) & 0xFF));
                    }
                    else
                    {
                        writer.Write((byte)(2 + Commands[i].Value.Length));
                    }

                    writer.Write(Commands[i].Value);
                    position = (uint)stream.Position;
                }
            }

            switch (Version)
            {
                case 0x9795:
                    for (var i = 8; i < bytes.Length; i++) bytes[i] = (byte)(bytes[i] << 2 | bytes[i] >> 6);
                    break;
                case 0xD197:
                    for (var i = 8; i < bytes.Length; i++) bytes[i] = (byte)~bytes[i];
                    break;
                case 0xCE89:
                    for (var i = 8; i < bytes.Length; i++) bytes[i] = (byte)(bytes[i] ^ Key);
                    break;
            }

            return bytes;
        }

        public static byte[][] Decode(byte[] data, int index)
        {
            var lines = new List<byte[]>();
            var buffer = new List<byte>();
            var offset = index;
            while (offset < data.Length)
            {
                switch (data[offset])
                {
                    case 0x01:
                        offset += 1 + 4;
                        break;
                    case 0x04:
                        offset += 1 + 1;
                        break;
                    case 0x08:
                        offset += 1 + 4;
                        break;
                    case 0x09:
                        offset += 1 + 1;
                        break;
                    case 0x0A:
                        offset += 1 + 4;
                        break;
                    case 0x0B:
                    case 0x0C:
                    case 0x10:
                        offset += 1 + 2;
                        break;
                    case 0x11:
                        offset += 1 + 4;
                        break;
                    case 0xFF:
                        offset += 1;
                        buffer.Clear();
                        while (offset < data.Length)
                        {
                            switch (data[offset])
                            {
                                case 0x00:
                                    offset = data.Length;
                                    continue;
                                case 0x5C:
                                    buffer.Add(Kana[0xB8]);
                                    offset += 1;
                                    if (data[offset] != 0x00) offset += 1;
                                    buffer.Add(Kana[data[offset - 1] * 2 + 1]);
                                    offset += 1;
                                    continue;
                                case 0x7F:
                                    buffer.Add(data[offset + 1]);
                                    offset += 2;
                                    continue;
                                default:
                                    if (data[offset] > 0x7F)
                                    {
                                        buffer.Add(data[offset]);
                                        buffer.Add(data[offset + 1]);
                                        offset += 2;
                                    }
                                    else
                                    {
                                        buffer.Add(Kana[data[offset] * 2]);
                                        buffer.Add(Kana[data[offset] * 2 + 1]);
                                        offset += 1;
                                    }

                                    continue;
                            }
                        }

                        lines.Add(buffer.ToArray());
                        break;
                    default:
                        offset += 1;
                        break;
                }
            }

            return lines.ToArray();
        }

        public static byte[] Encode(byte[] data, int index, byte[][] messages)
        {
            var offset = index;
            var buffer = new List<byte>();
            var k = 0;
            while (offset < data.Length)
            {
                var pos = offset;

                switch (data[offset])
                {
                    case 0x01:
                        offset += 1 + 4;
                        break;
                    case 0x04:
                        offset += 1 + 1;
                        break;
                    case 0x08:
                        offset += 1 + 4;
                        break;
                    case 0x09:
                        offset += 1 + 1;
                        break;
                    case 0x0A:
                        offset += 1 + 4;
                        break;
                    case 0x0B:
                    case 0x0C:
                    case 0x10:
                        offset += 1 + 2;
                        break;
                    case 0x11:
                        offset += 1 + 4;
                        break;
                    case 0xFF:
                        offset += 1;

                        while (offset < data.Length)
                        {
                            if (data[offset] == 0x00)
                            {
                                offset++;
                                break;
                            }
                            switch (data[offset])
                            {
                                case 0x5C:
                                case 0x7F:
                                    offset += 2;
                                    break;
                                default:
                                    offset += data[offset] > 0x7F ? 2 : 1;
                                    break;
                            }
                        }

                        buffer.Add(0xFF);
                        
                        for (var i = 0; i < messages[k].Length; i++)
                        {
                            if (messages[k][i] <= 0x7F)
                            {
                                buffer.Add(0x7F);
                                buffer.Add(messages[k][i]);
                                continue;
                            }

                            var t = 0;
                            for (var j = 2; j < 0x7F * 2; j += 2)
                            {
                                if (messages[k][i] != Kana[j] || messages[k][i + 1] != Kana[j + 1]) continue;
                                t = j / 2;
                                break;
                            }

                            if (t != 0)
                            {
                                buffer.Add((byte)t);
                                if (t == 0x5C) buffer.Add(0x00);
                            }
                            else
                            {
                                buffer.Add(messages[k][i]);
                                buffer.Add(messages[k][i + 1]);
                            }

                            i++;
                        }

                        buffer.Add(0x00);

                        k++;
                        continue;
                    default:
                        offset += 1;
                        break;
                }

                for (var i = pos; i < offset; i++)
                {
                    buffer.Add(data[i]);
                }
            } 

            Array.Resize(ref data, index + buffer.Count);
            buffer.CopyTo(data, index);
            return data;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        public enum Instruction
        {
            ED = 0x00, // 終了
            LS = 0x01, // シナリオのロード実行
            LSBS = 0x02, // サブシナリオのロード実行
            SRET = 0x03, // サブシナリオからの復帰
            JP = 0x04, // ジャンプ
            JS = 0x05, // サブルーチンジャンプ
            RT = 0x06, // サブルーチンから復帰
            ONJP = 0x07, // 条件ジャンプ
            ONJS = 0x08, // 条件サブルーチン呼びだし
            CHILD = 0x09, // 子プロセスの実行
            URL = 0x0A,
            UNK_0B = 0x0B,
            UNK_0C = 0x0C,
            UNK_0D = 0x0D,
            UNK_0E = 0x0E,
            UNK_0F = 0x0F,
            CW = 0x10, // コマンドウィンドウの位置、横サイズセット
            CP = 0x11, // コマンドウィンドウのフレーム読み込み
            CIR = 0x12, // アイコン読み込み
            CPS = 0x13, // 文字パレット設定
            CIP = 0x14, // コマンドにアイコンセット
            CSET = 0x15, // コマンドの名前セット
            CWO = 0x16, // コマンドウィンドウのオープン
            CWC = 0x17, // コマンドウィンドウのクローズ
            CC = 0x18, // コマンド選択実行
            CCLR = 0x19,
            CRESET = 0x1A, // コマンドの名前設定の準備
            CRND = 0x1B, // コマンドのランダム配置
            CTEXT = 0x1C, // テキスト表示
            UNK_1D = 0x1D,
            UNK_1E = 0x1E,
            UNK_1F = 0x1F,
            WS = 0x20, // ウィンドウ表示位置設定
            WP = 0x21, // ウィンドウパーツ読み込み
            WL = 0x22, // クリック待パーツ読み込み
            WW = 0x23, // クリック待設定
            CN = 0x24, // 人物名文字数設定
            CNS = 0x25, // 人物名セット
            PF = 0x26, // メッセージ表示スピード設定
            PB = 0x27, // 文字の大きさ指定
            PJ = 0x28, // 文字の形態設定
            WO = 0x29, // ウィンドウオープン
            WC = 0x2A, // ウィンドウのクローズ
            PM = 0x2B, // 文字の表示
            PMP = 0x2C, // 音声フラグチェック付き文字の表示
            WSH = 0x2D, // メッセージウィンドウの非表示
            WSS = 0x2E, // メッセージウィンドウの表示
            UNK_2F = 0x2F,
            FLN = 0x30, // フラグ数の設定
            SK = 0x31, // フラグのセット・クリア・反転
            SKS = 0x32, // フラグをまとめてセット・クリア・反転
            HF = 0x33, // フラグ判定ジャンプ
            FT = 0x34, // フラグ転送
            SP = 0x35, // パターンフラグのセット
            HP = 0x36, // パターンフラグ判定ジャンプ
            STS = 0x37, // システムフラグの設定
            ES = 0x38, // 指定フラグのセーブ
            EC = 0x39, // 指定フラグのロード
            STC = 0x3A, // システムフラグの判定ジャンプ
            HN = 0x3B, // フラグ判定ジャンプ
            HXP = 0x3C, // パターンフラグ判定ジャンプ２
            UNK_3D = 0x3D,
            UNK_3E = 0x3E,
            UNK_3F = 0x3F,
            HLN = 0x40, // 変数の数をセット
            HS = 0x41, // 変数に値を代入
            HINC = 0x42, // 変数をインクリメント
            HDEC = 0x43, // 変数をデクリメント
            CALC = 0x44, // 計算する
            HSG = 0x45, // 変数にまとめて値を代入
            HT = 0x46, // 変数の転送
            IF = 0x47, // IF-THEN の実行
            EXA = 0x48, // フラグと変数を別途に記憶する領域を確保します。
            EXS = 0x49, // EXA コマンドで確保した領域に指定のフラグ／変数を書き込みます。
            EXC = 0x4A, // EXA コマンドで確保した領域から指定のフラグ／変数に読み込みます。
            SCP = 0x4B, // システム変数のコピー
            SSP = 0x4C, // システム変数にパラメータをコピーする
            UNK_4D = 0x4D,
            UNK_4E = 0x4E,
            UNK_4F = 0x4F,
            VSET = 0x50, // 仮想ＶＲＡＭの設定
            GN = 0x51, // グラフィック表示オン
            GF = 0x52, // グラフィック表示オフ
            GC = 0x53, // グラフィッククリア
            GI = 0x54, // グラフィックフェードイン
            GO = 0x55, // グラフィックフェードアウト
            GL = 0x56, // グラフィックロード表示
            GP = 0x57, // グラフィックのコピー
            GB = 0x58, // 矩形を描画
            GPB = 0x59, // 文字サイズ設定
            GPJ = 0x5A, // 文字形態の設定
            PR = 0x5B, // 文字表示
            GASTAR = 0x5C, // アニメーションのスタート
            GASTOP = 0x5D, // アニメーションのストップ
            GPI = 0x5E, // グラフィックエフェクトとBGMのフェードイン
            GPO = 0x5F, // グラフィックエフェクトとBGMのフェードアウト
            GGE = 0x60, // グレースケールを使用したエフェクト
            GPE = 0x61, // 拡大・縮小処理
            GSCRL = 0x62, // スクロール処理
            GV = 0x63, // 画面揺らし処理
            GAL = 0x64, // アニメーションループ設定
            GAOPEN = 0x65, // アニメーションファイルのオープン
            GASET = 0x66, // アニメーションデータのセット
            GAPOS = 0x67, // アニメーションの表示位置のセット
            GACLOSE = 0x68, // アニメーションファイルのクローズ
            GADELETE = 0x69, // アニメーションの削除
            UNK_6A = 0x6A,
            UNK_6B = 0x6B,
            UNK_6C = 0x6C,
            UNK_6D = 0x6D,
            UNK_6E = 0x6E,
            SGL = 0x6F, // セーブイメージを読み込む
            ML = 0x70, // 音楽データのロード・再生
            MP = 0x71, // 音楽の再生
            MF = 0x72, // 音楽フェードアウト
            MS = 0x73, // 音楽のストップ
            SER = 0x74, // 効果音のロード
            SEP = 0x75, // 効果音の再生
            SED = 0x76, // 効果音の削除
            PCMON = 0x77, // PCM 音声の再生
            PCML = 0x78, // PCMのロード
            PCMS = 0x79, // PCMの停止
            PCMEND = 0x7A, // PCM 音声の停止待機
            SES = 0x7B, // SES 効果音の停止
            BGMGETPOS = 0x7C, // 音楽の再生位置取得
            SEGETPOS = 0x7D, // 効果音の再生位置取得
            PCMGETPOS = 0x7E, // PCMの再生位置取得
            PCMCN = 0x7F, // 音声ファイル名のバックアップ
            IM = 0x80, // マウスカーソルデータの読み込み
            IC = 0x81, // マウスカーソルの変更
            IMS = 0x82, // マウス移動範囲の設定
            IXY = 0x83, // マウスの位置変更
            IH = 0x84, // IG コマンドの選択範囲設定
            IG = 0x85, // 画面内マウス入力
            IGINIT = 0x86, // 画面内マウス入力－初期化
            IGRELEASE = 0x87, // 画面内マウス入力－解放
            IHK = 0x88, // キーボード拡張－移動先データの設定
            IHKDEF = 0x89, // キーボード拡張－デフォルト番号の設定
            IHGL = 0x8A, // 選択レイアウト画像イメージ読込
            IHGC = 0x8B, // 選択レイアウトゼロクリア
            IHGP = 0x8C, // 指定画像転送
            CLK = 0x8D, // クリック待ち
            IGN = 0x8E, // カーソルＮＯ取得
            UNK_8F = 0x8F,
            DAE = 0x90, // CDDAの設定
            DAP = 0x91, // CDDAの再生
            DAS = 0x92, // CDDAの停止
            UNK_93 = 0x93,
            UNK_94 = 0x94,
            UNK_95 = 0x95,
            UNK_96 = 0x96,
            UNK_97 = 0x97,
            UNK_98 = 0x98,
            UNK_99 = 0x99,
            UNK_9A = 0x9A,
            UNK_9B = 0x9B,
            UNK_9C = 0x9C,
            UNK_9D = 0x9D,
            UNK_9E = 0x9E,
            SETINSIDEVOL = 0x9F, // 内部音量設定
            KIDCLR = 0xA0, // 既読文章の初期化
            KIDMOJI = 0xA1, // 既読文章の文字の色を設定する
            KIDPAGE = 0xA2, // 既読文章の頁情報
            KIDSET = 0xA3, // 既読文章の既読フラグ判定
            KIDEND = 0xA4, // 既読文章の既読フラグ設定
            KIDFN = 0xA5, // 既読フラグ数設定
            KIDHABA = 0xA6, // 既読文章の１行あたりの文字数
            KIDSCAN = 0xA7, // 既読機能と既読フラグの判定
            UNK_A8 = 0xA8,
            UNK_A9 = 0xA9,
            UNK_AA = 0xAA,
            UNK_AB = 0xAB,
            UNK_AC = 0xAC,
            UNK_AD = 0xAD,
            SETKIDWNDPUTPOS = 0xAE, // 既読ウィンドウのプット位置指定
            SETMESWNDPUTPOS = 0xAF, // メッセージウィンドウのプット位置指定
            INNAME = 0xB0,
            NAMECOPY = 0xB1,
            CHANGEWALL = 0xB2,
            MSGBOX = 0xB3, // メッセージボックス表示
            SETSMPRATE = 0xB4, // サンプリングレート設定
            UNK_B5 = 0xB5,
            UNK_B6 = 0xB6,
            UNK_B7 = 0xB7,
            UNK_B8 = 0xB8,
            UNK_B9 = 0xB9,
            UNK_BA = 0xBA,
            UNK_BB = 0xBB,
            UNK_BC = 0xBC,
            CLKEXMCSET = 0xBD, // クリック待ち拡張機能のマウスカーソルＩＤ初期化
            IRCLK = 0xBE, //
            IROPN = 0xBF, //
            UNK_C0 = 0xC0,
            UNK_C1 = 0xC1,
            UNK_C2 = 0xC2,
            UNK_C3 = 0xC3,
            UNK_C4 = 0xC4,
            UNK_C5 = 0xC5,
            UNK_C6 = 0xC6,
            UNK_C7 = 0xC7,
            UNK_C8 = 0xC8,
            UNK_C9 = 0xC9,
            UNK_CA = 0xCA,
            UNK_CB = 0xCB,
            UNK_CC = 0xCC,
            UNK_CD = 0xCD,
            UNK_CE = 0xCE,
            UNK_CF = 0xCF,
            PPTL = 0xD0,
            PPABL = 0xD1,
            PPTYPE = 0xD2,
            PPORT = 0xD3,
            PPCRT = 0xD4,
            SABL = 0xD5,
            MPM = 0xD6, // 複数行同時表示の実行
            MPC = 0xD7, // 登録行の破棄
            PM2 = 0xD8,
            MPM2 = 0xD9,
            UNK_DA = 0xDA,
            UNK_DB = 0xDB,
            UNK_DC = 0xDC,
            UNK_DD = 0xDD,
            UNK_DE = 0xDE,
            UNK_DF = 0xDF,
            TAGSET = 0xE0, // ダイアログのタグの設定
            FRAMESET = 0xE1, // ダイアログのフレーム設定
            RBSET = 0xE2, // ダイアログのラジオボタン設定
            CBSET = 0xE3, // ダイアログのチェックボックス設定
            SLDRSET = 0xE4, // ダイアログのスライダー設定
            OPSL = 0xE5, // SAVE・LOADダイアログのオープン
            OPPROP = 0xE6, // 設定ダイアログのオープン
            DISABLE = 0xE7, // ダイアログコントロールのディセイブル
            ENABLE = 0xE8, // ダイアログコントロールのイネイブル
            TITLE = 0xE9,
            UNK_EA = 0xEA,
            UNK_EB = 0xEB,
            UNK_EC = 0xEC,
            UNK_ED = 0xED,
            UNK_EE = 0xEE,
            EXT = 0xEF, // 拡張処理
            CNF = 0xF0, // 連結ファイルのファイル名設定
            ATIMES = 0xF1, // ウェイトの開始
            AWAIT = 0xF2, // ウェイト待ち
            AVIP = 0xF3, // AVI ファイルの再生
            PPF = 0xF4, // ポップアップメニューの表示設定
            SVF = 0xF5, // セーブの可・不可の設定
            PPE = 0xF6, // ポップアップメニューの禁止・許可表示設定
            SETGAMEINFO = 0xF7, // ゲーム内情報の設定
            SETFONTSTYLE = 0xF8, // 表示フォントスタイル指定
            SETFONTCOLOR = 0xF9, // 表示フォントカラー指定
            TIMERSET = 0xFA, // タイムカウンター設定
            TIMEREND = 0xFB, // タイムカウンター終了
            TIMERGET = 0xFC, // タイムカウンター取得
            GRPOUT = 0xFD, // 画像出力
            BREAK = 0xFE, // Ｂｒｅａｋ
            EXT_ = 0xFF, // 拡張処理
        }

        private static readonly byte[] Kana =
        {
            0x81, 0x40, 0x81, 0x40, 0x81, 0x41, 0x81, 0x42,
            0x81, 0x45, 0x81, 0x48, 0x81, 0x49, 0x81, 0x69,
            0x81, 0x6a, 0x81, 0x75, 0x81, 0x76, 0x82, 0x4f,
            0x82, 0x50, 0x82, 0x51, 0x82, 0x52, 0x82, 0x53,
            0x82, 0x54, 0x82, 0x55, 0x82, 0x56, 0x82, 0x57,
            0x82, 0x58, 0x82, 0xa0, 0x82, 0xa2, 0x82, 0xa4,
            0x82, 0xa6, 0x82, 0xa8, 0x82, 0xa9, 0x82, 0xaa,
            0x82, 0xab, 0x82, 0xac, 0x82, 0xad, 0x82, 0xae,
            0x81, 0x40, 0x82, 0xb0, 0x82, 0xb1, 0x82, 0xb2,
            0x82, 0xb3, 0x82, 0xb4, 0x82, 0xb5, 0x82, 0xb6,
            0x82, 0xb7, 0x82, 0xb8, 0x82, 0xb9, 0x82, 0xba,
            0x82, 0xbb, 0x82, 0xbc, 0x82, 0xbd, 0x82, 0xbe,
            0x82, 0xbf, 0x82, 0xc0, 0x82, 0xc1, 0x82, 0xc2,
            0x82, 0xc3, 0x82, 0xc4, 0x82, 0xc5, 0x82, 0xc6,
            0x82, 0xc7, 0x82, 0xc8, 0x82, 0xc9, 0x82, 0xca,
            0x82, 0xcb, 0x82, 0xcc, 0x82, 0xcd, 0x82, 0xce,
            0x82, 0xd0, 0x82, 0xd1, 0x82, 0xd3, 0x82, 0xd4,
            0x82, 0xd6, 0x82, 0xd7, 0x82, 0xd9, 0x82, 0xda,
            0x82, 0xdc, 0x82, 0xdd, 0x82, 0xde, 0x82, 0xdf,
            0x82, 0xe0, 0x82, 0xe1, 0x82, 0xe2, 0x82, 0xe3,
            0x82, 0xe4, 0x82, 0xe5, 0x82, 0xe6, 0x82, 0xe7,
            0x82, 0xe8, 0x82, 0xe9, 0x82, 0xea, 0x82, 0xeb,
            0x82, 0xed, 0x82, 0xf0, 0x82, 0xf1, 0x83, 0x41,
            0x83, 0x43, 0x83, 0x45, 0x83, 0x47, 0x83, 0x49,
            0x83, 0x4a, 0x83, 0x4c, 0x83, 0x4e, 0x83, 0x50,
            0x83, 0x52, 0x83, 0x54, 0x83, 0x56, 0x83, 0x58,
            0x83, 0x5a, 0x83, 0x5c, 0x83, 0x5e, 0x83, 0x60,
            0x83, 0x62, 0x83, 0x63, 0x83, 0x65, 0x83, 0x67,
            0x83, 0x69, 0x83, 0x6a, 0x82, 0xaf, 0x83, 0x6c,
            0x83, 0x6d, 0x83, 0x6e, 0x83, 0x71, 0x83, 0x74,
            0x83, 0x77, 0x83, 0x7a, 0x83, 0x7d, 0x83, 0x7e,
            0x83, 0x80, 0x83, 0x81, 0x83, 0x82, 0x83, 0x84
        };
    }
}