using System;
using System.IO;

namespace ATool
{
    public static class ATool
    {
        #region Text
        
        public static byte[] TrimEnd(this byte[] source)
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

        public static string PatchFileName(this string path, string version)
        {
            return $"{Path.GetFileNameWithoutExtension(path)}_{version}{Path.GetExtension(path)}";
        }

        public static string ReplaceGbkUnsupported(this string source, bool force = true)
        {
            var chars = source
                .Replace("ﾃﾞﾊﾞｯｸﾞ", force ? "偵錯   " : "Debug  ")
                .Replace("ﾒｯｾｰｼﾞ", force ? "伝言  " : "MSG   ") // Message
                .Replace("ﾊﾟﾗﾒｰﾀ", force ? "助変数" : "PARAM ") // 媒介変数 / Parameter
                .Replace("ｽﾋﾟｰﾄﾞ", force ? "速度  " : "Speed ")
                .Replace("ﾀｲﾐﾝｸﾞ", force ? "時機  " : "Timing")
                .Replace("ｳｨﾝﾄﾞｳ", force ? "窓口  " : "Window")
                .Replace("ｲﾍﾞﾝﾄ", force ? "事件 " : "Event")
                .Replace("ｽｷｯﾌﾟ", force ? "躍進 " : "Skip ")
                .Replace("ｺﾒﾝﾄ", force ? "評釈" : "COMM") // Comment
                .Replace("ﾌｧｲﾙ", force ? "文卷" : "File")
                .Replace("ﾌﾗｸﾞ", force ? "旗幟" : "Flag")
                .Replace("ｹﾞｰﾑ", force ? "遊戯" : "Game")
                .Replace("ﾛｰﾄﾞ", force ? "荷込" : "Load")
                .Replace("ﾒﾆｭｰ", force ? "菜単" : "Menu")
                .Replace("ｾｰﾌﾞ", force ? "保存" : "Save")
                .Replace("ｻｲｽﾞ", force ? "号数" : "Size")
                .Replace("ｷｰ", force ? "鍵" : "Ｋ") // Key
                .Replace("⇒", "=>") // '\u226A'
                .Replace("≪", "<<") // '\u226A'
                .Replace("≫", ">>") // '\u226B'
                .Replace("㌶", "ha") // '\u3336'
                .Replace("㍊", "mb") // '\u334A'
                .Replace("㏍", "KK") // '\u33CD'
                .ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = chars[i] switch
                {
                    '\u00B4' => '\u02CA', // '´' => 'ˊ'
                    '\u00B6' => '\u25A1', // '¶'
                    '\u2020' => force ? '\u5341' : '\u25A1', // '†' => '十'
                    '\u2021' => force ? '\u2260' : '\u25A1', // '‡' => '≠'
                    '\u212B' => force ? '\u57C3' : '\u25A1', // 'Å' => '埃'
                    '\u21D2' => force ? '\u2192' : '\u25A1', // '⇒' => '→'
                    '\u21D4' => '\u25A1', // '⇔'
                    '\u2200' => '\u25A1', // '∀'
                    '\u2202' => '\u25A1', // '∂'
                    '\u2203' => '\u25A1', // '∃'
                    '\u2207' => '\u25A1', // '∇'
                    '\u220B' => '\u25A1', // '∋'
                    '\u222C' => '\u25A1', // '∬'
                    '\u226A' => force ? '\u300A' : '\u25A1', // '≪' => '《'
                    '\u226B' => force ? '\u300B' : '\u25A1', // '≫' => '》'
                    '\u2282' => '\u25A1', // '⊂'
                    '\u2283' => '\u25A1', // '⊃'
                    '\u2286' => '\u25A1', // '⊆'
                    '\u2287' => '\u25A1', // '⊇'
                    '\u246A' => '\u247E', // '⑪' => '⑾'
                    '\u246B' => '\u247F', // '⑫' => '⑿'
                    '\u246C' => '\u2480', // '⑬' => '⒀'
                    '\u246D' => '\u2481', // '⑭' => '⒁'
                    '\u246E' => '\u2482', // '⑮' => '⒂'
                    '\u246F' => '\u2483', // '⑯' => '⒃'
                    '\u2470' => '\u2484', // '⑰' => '⒄'
                    '\u2471' => '\u2485', // '⑱' => '⒅'
                    '\u2472' => '\u2486', // '⑲' => '⒆'
                    '\u2473' => '\u2487', // '⑳' => '⒇'
                    '\u25EF' => force ? '\u3007' : '\u25A1', // '◯' => '〇'
                    '\u266A' => force ? '\u624C' : '\u25A1', // '♪' => '扌'
                    '\u266D' => force ? '\uFF42' : '\u25A1', // '♭' => 'ｂ'
                    '\u266F' => force ? '\uFF03' : '\u25A1', // '♯' => '＃'
                    '\u301F' => force ? '\u309B' : '\u25A1', // '〟' => '゛'
                    '\u30FB' => '\u00B7', // '・' => '·'
                    '\u3232' => force ? '\u6709' : '\u25A1', // '㈲' => '有'
                    '\u3239' => force ? '\u4EE3' : '\u25A1', // '㈹' => '代'
                    '\u32A4' => force ? '\u4E0A' : '\u25A1', // '㊤' => '上'
                    '\u32A5' => force ? '\u4E2D' : '\u25A1', // '㊥' => '中'
                    '\u32A6' => force ? '\u4E0B' : '\u25A1', // '㊦' => ‘下’
                    '\u32A7' => force ? '\u5DE6' : '\u25A1', // '㊧' => '左'
                    '\u32A8' => force ? '\u53F3' : '\u25A1', // '㊨' => '右'

                    #region 缩写

                    '\u3303' => force ? '\u755D' : '\u25A1', // '㌃' => '畝' are
                    '\u330D' => force ? '\u30AB' : '\u25A1', // '㌍' => 'カ' calorie
                    '\u3314' => force ? '\u5343' : '\u25A1', // '㌔' => '千' kilo
                    '\u3318' => force ? '\uFF47' : '\u25A1', // '㌘' => 'ｇ' gram
                    '\u3322' => force ? '\u5398' : '\u25A1', // '㌢' => '厘' centi
                    '\u3323' => force ? '\uFFE0' : '\u25A1', // '㌣' => '￠' cent 
                    '\u3326' => force ? '\uFF04' : '\u25A1', // '㌦' => '＄' dollar
                    '\u3327' => force ? '\u5678' : '\u25A1', // '㌧' => '噸' ton
                    '\u332B' => force ? '\uFF05' : '\u25A1', // '㌫' => '％' percent
                    '\u3336' => '\u25A1', // '㌶' hectare
                    '\u333B' => force ? '\u9801' : '\u25A1', // '㌻' => '頁' page
                    '\u3349' => force ? '\u33D5' : '\u25A1', // '㍉' => '㏕' milli
                    '\u334A' => '\u25A1', // '㍊' millibar
                    '\u334B' => force ? '\u5146' : '\u25A1', // '㍋' => '兆' mega
                    '\u334D' => force ? '\uFF2D' : '\u25A1', // '㍍' => 'Ｍ' metre
                    '\u3351' => force ? '\uFF2C' : '\u25A1', // '㍑' => 'Ｌ' liter
                    '\u3357' => force ? '\uFF37' : '\u25A1', // '㍗' => 'Ｗ' watt
                    '\u337B' => force ? '\uFF28' : '\u25A1', // '㍻' => 'Ｈ'
                    '\u337C' => force ? '\uFF33' : '\u25A1', // '㍼' => 'Ｓ'
                    '\u337D' => force ? '\uFF34' : '\u25A1', // '㍽' => 'Ｔ'
                    '\u337E' => force ? '\uFF2D' : '\u25A1', // '㍾' => 'Ｍ'
                    '\u33CD' => '\u25A1', // '㏍'

                    #endregion

                    #region 错版字

                    '\uF929' => '\u6717', // '朗'
                    '\uF9DC' => '\u9686', // '隆'
                    '\uFA10' => '\u585A', // '塚'
                    '\uFA12' => '\u6674', // '晴'
                    '\uFA15' => '\u51DE', // '凞'
                    '\uFA16' => '\u732A', // '猪'
                    '\uFA17' => '\u76CA', // '益'
                    '\uFA19' => '\u795E', // '神'
                    '\uFA1A' => '\u7965', // '祥'
                    '\uFA1B' => '\u798F', // '福'
                    '\uFA1C' => '\u9756', // '靖'
                    '\uFA1D' => '\u7CBE', // '精'
                    '\uFA1E' => '\u7FBD', // '羽'
                    '\uFA22' => '\u8AF8', // '諸'
                    '\uFA25' => '\u9038', // '逸'
                    '\uFA26' => '\u90FD', // '都'
                    '\uFA2A' => '\u98EF', // '飯'
                    '\uFA2B' => '\u98FC', // '飼'
                    '\uFA2C' => '\u9928', // '館'
                    '\uFA2D' => '\u9DB4', // '鶴',

                    #endregion

                    #region 单字节

                    '\uFF61' => force ? '\u002E' : '\u0020', // '｡' => '.'
                    '\uFF64' => force ? '\u002C' : '\u0020', // '､' => ','
                    '\uFF65' => force ? '\u002D' : '\u0020', // '･' => '-'

                    #endregion

                    _ => chars[i]
                };
            }

            return new string(chars);
        }
        
        public static string ReplaceHalfWidthKana(this string source)
        {
            var chars = source.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = chars[i] switch
                {
                    '\uFF61' => '\u3002', // '｡' => '。'
                    '\uFF62' => '\u300C', // '｢' => '「'
                    '\uFF63' => '\u300D', // '｣' => '」'
                    '\uFF64' => '\u3001', // '､' => '、'
                    '\uFF65' => '\u30FB', // '･' => '・'
                    '\uFF66' => '\u30F2', // 'ｦ' => 'ヲ'
                    '\uFF67' => '\u30A1', // 'ｧ' => 'ァ'
                    '\uFF68' => '\u30A3', // 'ｨ' => 'ィ'
                    '\uFF69' => '\u30A5', // 'ｩ' => 'ゥ'
                    '\uFF6A' => '\u30A7', // 'ｪ' => 'ェ'
                    '\uFF6B' => '\u30A9', // 'ｫ' => 'ォ'
                    '\uFF6C' => '\u30E3', // 'ｬ' => 'ャ'
                    '\uFF6D' => '\u30E5', // 'ｭ' => 'ュ'
                    '\uFF6E' => '\u30E7', // 'ｮ' => 'ョ'
                    '\uFF6F' => '\u30C3', // 'ｯ' => 'ッ'
                    '\uFF70' => '\u30FC', // 'ｰ' => 'ー'
                    '\uFF71' => '\u30A2', // 'ｱ' => 'ア'
                    '\uFF72' => '\u30A4', // 'ｲ' => 'イ'
                    '\uFF73' => '\u30A6', // 'ｳ' => 'ウ'
                    '\uFF74' => '\u30A8', // 'ｴ' => 'エ'
                    '\uFF75' => '\u30AA', // 'ｵ' => 'オ'
                    '\uFF76' => '\u30AB', // 'ｶ' => 'カ'
                    '\uFF77' => '\u30AD', // 'ｷ' => 'キ'
                    '\uFF78' => '\u30AF', // 'ｸ' => 'ク'
                    '\uFF79' => '\u30B1', // 'ｹ' => 'ケ'
                    '\uFF7A' => '\u30B3', // 'ｺ' => 'コ'
                    '\uFF7B' => '\u30B5', // 'ｻ' => 'サ'
                    '\uFF7C' => '\u30B7', // 'ｼ' => 'シ'
                    '\uFF7D' => '\u30B9', // 'ｽ' => 'ス'
                    '\uFF7E' => '\u30BB', // 'ｾ' => 'セ'
                    '\uFF7F' => '\u30BD', // 'ｿ' => 'ソ'
                    '\uFF80' => '\u30BF', // 'ﾀ' => 'タ'
                    '\uFF81' => '\u30C1', // 'ﾁ' => 'チ'
                    '\uFF82' => '\u30C4', // 'ﾂ' => 'ツ'
                    '\uFF83' => '\u30C6', // 'ﾃ' => 'テ'
                    '\uFF84' => '\u30C8', // 'ﾄ' => 'ト'
                    '\uFF85' => '\u30CA', // 'ﾅ' => 'ナ'
                    '\uFF86' => '\u30CB', // 'ﾆ' => 'ニ'
                    '\uFF87' => '\u30CC', // 'ﾇ' => 'ヌ'
                    '\uFF88' => '\u30CD', // 'ﾈ' => 'ネ'
                    '\uFF89' => '\u30CE', // 'ﾉ' => 'ノ'
                    '\uFF8A' => '\u30CF', // 'ﾊ' => 'ハ'
                    '\uFF8B' => '\u30D2', // 'ﾋ' => 'ヒ'
                    '\uFF8C' => '\u30D5', // 'ﾌ' => 'フ'
                    '\uFF8D' => '\u30D8', // 'ﾍ' => 'ヘ'
                    '\uFF8E' => '\u30DB', // 'ﾎ' => 'ホ'
                    '\uFF8F' => '\u30DE', // 'ﾏ' => 'マ'
                    '\uFF90' => '\u30DF', // 'ﾐ' => 'ミ'
                    '\uFF91' => '\u30E0', // 'ﾑ' => 'ム'
                    '\uFF92' => '\u30E1', // 'ﾒ' => 'メ'
                    '\uFF93' => '\u30E2', // 'ﾓ' => 'モ'
                    '\uFF94' => '\u30E4', // 'ﾔ' => 'ヤ'
                    '\uFF95' => '\u30E6', // 'ﾕ' => 'ユ'
                    '\uFF96' => '\u30E8', // 'ﾖ' => 'ヨ'
                    '\uFF97' => '\u30E9', // 'ﾗ' => 'ラ'
                    '\uFF98' => '\u30EA', // 'ﾘ' => 'リ'
                    '\uFF99' => '\u30EB', // 'ﾙ' => 'ル'
                    '\uFF9A' => '\u30EC', // 'ﾚ' => 'レ'
                    '\uFF9B' => '\u30ED', // 'ﾛ' => 'ロ'
                    '\uFF9C' => '\u30EF', // 'ﾜ' => 'ワ'
                    '\uFF9D' => '\u30F3', // 'ﾝ' => 'ン'
                    '\uFF9E' => '\u309B', // 'ﾞ' => '゛'
                    '\uFF9F' => '\u309C', // 'ﾟ' => '゜'
                    _ => chars[i]
                };
            }

            return new string(chars);
        }
        
        #endregion

        #region Bin
        
        public static void CopyOverlapped(this byte[] data, int src, int dst, int count)
        {
            if (dst > src)
            {
                while (count > 0)
                {
                    var preceding = Math.Min(dst - src, count);
                    Buffer.BlockCopy(data, src, data, dst, preceding);
                    dst += preceding;
                    count -= preceding;
                }
            }
            else
            {
                Buffer.BlockCopy(data, src, data, dst, count);
            }
        }

        public static void Rot(this byte[] source)
        {
            for (var i = 0; i < source.Length; i++)
            {
                source[i] = (byte)((source[i] >> 0x04) | (source[i] << 0x04));
            }
        }

        #endregion
    }
}