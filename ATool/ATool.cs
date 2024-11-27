using System;

namespace ATool
{
    public static class ATool
    {
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

        public static void Rot(this byte[] source)
        {
            for (var i = 0; i < source.Length; i++)
            {
                source[i] = (byte)((source[i] >> 0x04) | (source[i] << 0x04));
            }
        }

        public static string ReplaceGbkUnsupported(this string source, bool force = true)
        {
            var chars = source
                .Replace("ﾃﾞﾊﾞｯｸﾞ", force ? "偵錯   " : "Debug  ")
                .Replace("ﾌｧｲﾙ", force ? "文卷" : "File")
                .Replace("ﾊﾟﾗﾒｰﾀ", force ? "助変数" : "Param ") // 媒介変数 / Parameter
                .Replace("ｽｷｯﾌﾟ", force ? "躍進 " : "Skip ")
                .Replace("ﾒﾆｭｰ", force ? "菜単" : "Menu")
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
    }
}