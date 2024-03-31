using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BlockifyLib.Launcher
{
    public static class Mapper
    {
        private static readonly Regex argBracket = new Regex(@"\$?\{(.*?)}");

        public static string[] Map(string[] arg, Dictionary<string, string?> dicts, string prepath)
        {
            List<string> args = new List<string>(arg.Length);
            foreach (string item in arg)
                args.Add(HandleEmptyArg(!string.IsNullOrEmpty(prepath) ?
                    ToFullPath(Interpolation(item, dicts, false), prepath) : Interpolation(item, dicts, false)));

            return args.ToArray();
        }

        public static string[] MapInterpolation(string[] arg, Dictionary<string, string?> dicts)
        {
            List<string> args = new List<string>(arg.Length);
            foreach (string item in arg)
                args.Add(Interpolation(item, dicts, true));

            return args.ToArray();
        }

        public static string[] MapPathString(string[] arg, string prepath)
        {
            List<string> args = new List<string>(arg.Length);
            foreach (string item in arg)
                args.Add(HandleEmptyArg(ToFullPath(item, prepath)));

            return args.ToArray();
        }

        public static string Interpolation(string str, Dictionary<string, string?> dicts, bool handleEmpty)
        {
            str = argBracket.Replace(str, (match =>
            {
                if (match.Groups.Count < 2)
                    return match.Value;

                if (dicts.TryGetValue(match.Groups[1].Value, out string? value))
                    return value == null ? value = "" : value;

                return match.Value;
            }));

            return handleEmpty ? HandleEmptyArg(str) : str;
        }

        public static string ToFullPath(string str, string prepath)
        {

            if (str.StartsWith("[") && str.EndsWith("]") && !string.IsNullOrEmpty(prepath))
            {
                string[] innerStr = str.TrimStart('[').TrimEnd(']').Split('@');
                string pathName = innerStr[0];
                string extension = "jar";

                if (innerStr.Length > 1)
                    extension = innerStr[1];

                return Path.Combine(prepath,
                    PackageName.Parse(pathName).GetPath(null, extension));
            }
            else if (str.StartsWith("\'") && str.EndsWith("\'"))
                return str.Trim('\'');
            else
                return str;
        }

        static string replaceByPos(string input, string replace, int startIndex, int length) =>
            replaceByPos(new StringBuilder(input), replace, startIndex, length);

        static string replaceByPos(StringBuilder sb, string replace, int startIndex, int length) =>
            sb.Remove(startIndex, length).Insert(startIndex, replace).ToString();

        public static string HandleEmptyArg(string input)
        {
            if (input.Contains("="))
            {
                var s = input.Split('=');

                if (s[1].Contains(" ") && !checkEmptyHandled(s[1]))
                    return s[0] + "=\"" + s[1] + "\"";
                else
                    return input;
            }
            else if (input.Contains(" ") && !checkEmptyHandled(input))
                return "\"" + input + "\"";
            else
                return input;
        }

        static bool checkEmptyHandled(string str) =>
            str.StartsWith("\"") || str.EndsWith("\"");
    }

    public class PackageName
    {
        public static PackageName Parse(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            string[] spliter = name.Split(':');
            if (name.Split(':').Length < 3)
                throw new ArgumentException("invalid name");

            return new PackageName(spliter);
        }

        private PackageName(string[] names)
        {
            this.names = names;
        }

        private readonly string[] names;

        public string this[int index] => names[index];

        public string Package => names[0];
        public string Name => names[1];
        public string Version => names[2];

        public string GetPath() =>
            GetPath("");

        public string GetPath(string? nativeId) =>
            GetPath(nativeId, "jar");

        public string GetPath(string? nativeId, string extension)
        {
            string filename = string.Join("-", names, 1, names.Length - 1);

            if (!string.IsNullOrEmpty(nativeId))
                filename += "-" + nativeId;
            filename += "." + extension;

            return Path.Combine(GetDirectory(), filename);
        }

        public string GetDirectory() =>
            Path.Combine(Package.Replace(".", "/"), Name, Version);

        public string GetClassPath() =>
            Package + "." + Name;
    }
}
