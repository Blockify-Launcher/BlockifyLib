using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace BlockifyLib.Launcher
{
    public class Library
    {
        public bool IsNative    { get; set; }

        public string Name      { get;  set; }
        public string Path      { get; set; }
        public string Url       { get; set; }
        public bool IsRequire   { get; set; } = true;

        public string Hash      { get; set; } = "";

        public class Parser
        {
            public static bool CheckOSRules = true;
            static string DefaultLibraryServer = "https://libraries.minecraft.net/";

            public static Library[]? ParseJson(JObject item)
            {
                List<Library> list = new List<Library>(item.Count);
                try
                {
                    bool isRequire;
                    JToken rules = item["rules"];
                    if (CheckOSRules && item["rules"] != null)
                        isRequire = Rule.CheckOSRequire((JArray)rules);

                    string req = item["clientreq"]?.ToString();
                    if (req != null && req.ToLower() != "true")
                        isRequire = false;

                    JToken artifact = item["artifact"] ?? item["downloads"]?["artifact"];
                    JToken classifiers = item["classifies"] ?? item["downloads"]?["classifiers"];
                    JToken natives = item["natives"];

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

                    if (artifact != null)
                        list.Add(createMLibrary(item["name"]?.ToString(), "", (JObject)artifact));

                    if (classifiers == null && artifact == null)
                        list.Add(createMLibrary(item["name"]?.ToString(), "", item));

                    return list.ToArray();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return null;
                }
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

                Library library = new Library
                {
                    Hash = hash?.ToString() ?? "",
                    IsNative = nativeId != "",
                    Name = name,
                    Path = System.IO.Path.Combine(Minecraft.Minecraft.Library, path),
                    Url = url
                };

                return library;
            }
        }
    }

}
