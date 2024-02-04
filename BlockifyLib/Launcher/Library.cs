using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace BlockifyLib.BlockifyLib.Launcher
{
    public class Library
    {
        private Library() { }

        public bool IsNative { get; private set; }

        public string Name { get; private set; }
        public string Path { get; private set; }
        public string Url { get; private set; }
        public bool IsRequire { get; private set; } = true;

        public string Hash { get; private set; } = "";

        internal class Parser
        {
            public static bool CheckOSRules = true;
            static string DefaultLibraryServer = "https://libraries.minecraft.net/";

            public static Library[] ParseJson(JArray json)
            {
                var ruleChecker = new Rule();
                var list = new List<Library>(json.Count);
                foreach (JObject item in json)
                    try
                    {
                        /* check rules array */
                        JToken rules = item["rules"];
                        if (CheckOSRules && item["rules"] != null)
                        {
                            bool isRequire = ruleChecker.CheckOSRequire((JArray)rules);
                            if (!isRequire)
                                continue;
                        }

                        /* forge clientreq */
                        string req = item["clientreq"]?.ToString();
                        if (req != null && req.ToLower() != "true")
                            continue;

                        /* support TLauncher */
                        JToken artifact = item["artifact"] ?? item["downloads"]?["artifact"];
                        JToken classifiers = item["classifies"] ?? item["downloads"]?["classifiers"];
                        JToken natives = item["natives"];

                        /* NATIVE library */
                        if (classifiers != null)
                        {
                            string? nativeId = "";

                            if (natives != null)
                                nativeId = natives[Rule.OSName]?.ToString();

                            if (nativeId != null && classifiers[nativeId] != null)
                                list.Add(createMLibrary(item["name"]?.ToString(),
                                            nativeId = nativeId.Replace("${arch}", Rule.Arch),
                                            (JObject)classifiers[nativeId]));
                        }

                        /* COMMON library */
                        if (artifact != null)
                            list.Add(createMLibrary(item["name"]?.ToString(), "", (JObject)artifact));

                        /* library */
                        if (classifiers == null && artifact == null)
                            list.Add(createMLibrary(item["name"]?.ToString(), "", item));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }

                return list.ToArray();
            }

            private static string NameToPath(string name, string native)
            {
                try
                {
                    string[] tmp = name.Split(':');
                    string front = tmp[0].Replace('.', '/');
                    string back = name.Substring(name.IndexOf(':') + 1);

                    string libpath = front + "/" + back.Replace(':', '/') + "/" + back.Replace(':', '-');

                    if (native != "")
                        libpath += "-" + native + ".jar";
                    else
                        libpath += ".jar";
                    return libpath;
                }
                catch
                {
                    return "";
                }
            }

            private static Library createMLibrary(string name, string nativeId, JObject job)
            {
                var path = job["path"]?.ToString();
                if (path == null || path == "")
                    path = NameToPath(name, nativeId);

                var url = job["url"]?.ToString();
                if (url == null)
                    url = DefaultLibraryServer + path;
                else if (url.Split('/').Last() == "")
                    url += path;

                JToken hash = job["sha1"] ?? job["checksums"]?[0];

                var library = new Library();
                library.Hash = hash?.ToString() ?? "";
                library.IsNative = (nativeId != "");
                library.Name = name;
                library.Path = System.IO.Path.Combine(Minecraft.Minecraft.Library, path);
                library.Url = url;

                return library;
            }
        }

    }
}
