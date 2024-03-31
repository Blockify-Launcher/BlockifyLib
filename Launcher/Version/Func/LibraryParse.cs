using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockifyLib.Launcher.Version.Func
{
    public class LibraryParse 
    { 
        public bool CheckOSRules { get; set; } = true;

        public Library[]? ParseJsonObject(JObject item)
        {
            try
            {
                var list = new List<Library>(2);

                var name = item["name"]?.ToString();
                var isRequire = true;

                // check rules array
                var rules = item["rules"];
                if (CheckOSRules && rules != null)
                    isRequire = Rule.CheckOSRequire((JArray)rules);

                // forge clientreq
                var req = item["clientreq"]?.ToString();
                if (req != null && req.ToLower() != "true")
                    isRequire = false;

                // support TLauncher
                var artifact = item["artifact"] ?? item["downloads"]?["artifact"];
                var classifiers = item["classifies"] ?? item["downloads"]?["classifiers"];
                var natives = item["natives"];

                // NATIVE library
                if (natives != null)
                {
                    var nativeId = natives[Rule.OSName]?.ToString().Replace("${arch}", Rule.Arch);

                    if (classifiers != null && nativeId != null)
                    {
                        JToken? lObj = classifiers[nativeId] ?? classifiers[Rule.OSName];
                        if (lObj != null)
                            list.Add(createLibrary(name, nativeId, isRequire, (JObject)lObj));
                    }
                    else
                        list.Add(createLibrary(name, nativeId, isRequire, new JObject()));
                }

                // COMMON library
                if (artifact != null)
                {
                    Library obj = createLibrary(name, "", isRequire, (JObject)artifact);
                    list.Add(obj);
                }

                // library
                if (artifact == null && natives == null)
                {
                    Library obj = createLibrary(name, "", isRequire, item);
                    list.Add(obj);
                }

                return list.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return null;
            }
        }

        private Library createLibrary(string? name, string? nativeId, bool require, JObject job)
        {
            string? path = job["path"]?.ToString();
            if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(name))
                path = PackageName.Parse(name).GetPath(nativeId);

            var hash = job["sha1"] ?? job["checksums"]?[0];

            long size = 0;
            string? sizeStr = job["size"]?.ToString();
            if (!string.IsNullOrEmpty(sizeStr))
                long.TryParse(sizeStr, out size);

            return new Library
            {
                Hash = hash?.ToString(),
                IsNative = !string.IsNullOrEmpty(nativeId),
                Name = name,
                Path = path,
                Size = size,
                Url = job["url"]?.ToString(),
                IsRequire = require
            };
        }
    
    } 
}
