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

        public static string ReplaceGbkUnsupported(this string source, bool force = true)
        {
            var chars = source.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = chars[i] switch
                {
                    '\u30FB' => '\u00B7', // '・' => '·'
                    '\u00B4' => '\u02CA', // '´' => 'ˊ'
                    '\u220B' => '\u25A1', // '∋'
                    '\u2286' => '\u25A1', // '⊆'
                    '\u2287' => '\u25A1', // '⊇'
                    '\u2282' => '\u25A1', // '⊂'
                    '\u2283' => '\u25A1', // '⊃'
                    '\u21D2' => force ? '\u2192' : '\u25A1', // '⇒' => '→'
                    '\u21D4' => '\u25A1', // '⇔'
                    '\u2200' => '\u25A1', // '∀'
                    '\u2203' => '\u25A1', // '∃'
                    '\u2202' => '\u25A1', // '∂'
                    '\u2207' => '\u25A1', // '∇'
                    '\u226A' => force ? '\u300A' : '\u25A1', // '≪' => '《'
                    '\u226B' => force ? '\u300B' : '\u25A1', // '≫' => '》'
                    '\u222C' => '\u25A1', // '∬'
                    '\u212B' => '\u25A1', // 'Å'
                    '\u266F' => '\u25A1', // '♯'
                    '\u266D' => '\u25A1', // '♭'
                    '\u266A' => '\u25A1', // '♪'
                    '\u2020' => '\u25A1', // '†'
                    '\u2021' => '\u25A1', // '‡'
                    '\u00B6' => '\u25A1', // '¶'
                    '\u25EF' => force ? '\u3007' : '\u25A1', // '◯' => '〇'
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
                    '\u3349' => '\u25A1', // '㍉'
                    '\u3314' => '\u25A1', // '㌔'
                    '\u3322' => '\u25A1', // '㌢'
                    '\u334D' => '\u25A1', // '㍍'
                    '\u3318' => '\u25A1', // '㌘'
                    '\u3327' => '\u25A1', // '㌧'
                    '\u3303' => '\u25A1', // '㌃'
                    '\u3336' => '\u25A1', // '㌶'
                    '\u3351' => '\u25A1', // '㍑'
                    '\u3357' => '\u25A1', // '㍗'
                    '\u330D' => '\u25A1', // '㌍'
                    '\u3326' => '\u25A1', // '㌦'
                    '\u3323' => '\u25A1', // '㌣'
                    '\u332B' => '\u25A1', // '㌫'
                    '\u334A' => '\u25A1', // '㍊'
                    '\u334B' => '\u25A1', // '㍋'
                    '\u333B' => '\u25A1', // '㌻'
                    '\u337B' => '\u25A1', // '㍻'
                    '\u301F' => '\u25A1', // '〟'
                    '\u33CD' => '\u25A1', // '㏍'
                    '\u32A4' => force ? '\u4E0A' : '\u25A1', // '㊤' => '上'
                    '\u32A5' => force ? '\u4E2D' : '\u25A1', // '㊥' => '中'
                    '\u32A6' => force ? '\u4E0B' : '\u25A1', // '㊦' => ‘下’
                    '\u32A7' => force ? '\u5DE6' : '\u25A1', // '㊧' => '左'
                    '\u32A8' => force ? '\u53F3' : '\u25A1', // '㊨' => '右'
                    '\u3232' => force ? '\u6709' : '\u25A1', // '㈲' => '有'
                    '\u3239' => force ? '\u4EE3' : '\u25A1', // '㈹' => '代'
                    '\u337E' => '\u25A1', // '㍾'
                    '\u337D' => '\u25A1', // '㍽'
                    '\u337C' => '\u25A1', // '㍼'
                    '\uFA10' => '\u585A', // '塚'
                    '\uFA12' => '\u6674', // '晴'
                    '\uF929' => '\u6717', // '朗'
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
                    '\uF9DC' => '\u9686', // '隆'
                    '\uFA2A' => '\u98EF', // '飯'
                    '\uFA2B' => '\u98FC', // '飼'
                    '\uFA2C' => '\u9928', // '館'
                    '\uFA2D' => '\u9DB4', // '鶴'
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