using System.Diagnostics.CodeAnalysis;
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
        }

        private static Encoding _encoding = Encoding.Default;
    }
}