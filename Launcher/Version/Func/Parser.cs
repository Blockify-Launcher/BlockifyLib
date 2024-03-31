using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace BlockifyLib.Launcher.Version.Func
{
    public class ParseException : Exception
    {
        public ParseException(string message) : base(message) { }
        public ParseException(Exception inner) : base("Failed to parse version", inner) { }
        public string? VersionName { get; internal set; }
    }

    public static class VersionParser
    {
        public static Version ParseFromFile(string path)
        {
            string json = File.ReadAllText(path);
            return ParseFromJson(json);
        }

        private static string GetObject(JObject obj, string name) =>
            obj[name]?.ToString();

        public static Version ParseFromJson(string json, bool writeProfile = true)
        {
            try
            {
                var job = JObject.Parse(json);

                // id
                var id = job["id"]?.ToString();
                if (string.IsNullOrEmpty(id))
                    throw new ParseException("Empty version id");

                var version = new Version(id);

                // javaVersion
                version.JavaVersion = job["javaVersion"]?["component"]?.ToString();

                // assets
                var assetindex = job["assetIndex"];
                var assets = job["assets"];
                if (assetindex != null)
                {
                    version.AssetId = assetindex["id"]?.ToString();
                    version.AssetUrl = assetindex["url"]?.ToString();
                    version.AssetHash = assetindex["sha1"]?.ToString();
                }
                else if (assets != null)
                    version.AssetId = assets.ToString();

                // client jar
                var client = job["downloads"]?["client"];
                if (client != null)
                {
                    version.ClientDownloadUrl = client["url"]?.ToString();
                    version.ClientHash = client["sha1"]?.ToString();
                }

                // libraries
                if (job["libraries"] is JArray libJArr)
                {
                    var libList = new List<Library>(libJArr.Count);
                    var libParser = new LibraryParse();
                    foreach (var item in libJArr)
                    {
                        var libs = libParser.ParseJsonObject((JObject)item);
                        if (libs != null)
                            libList.AddRange(libs);
                    }

                    version.Libraries = libList.ToArray();
                }

                // mainClass
                version.MainClass = job["mainClass"]?.ToString();

                // argument
                version.MinecraftArguments = job["minecraftArguments"]?.ToString();

                var ag = job["arguments"];
                if (ag != null)
                {
                    if (ag["game"] is JArray gameArg)
                        version.GameArguments = argParse(gameArg);
                    if (ag["jvm"] is JArray jvmArg)
                        version.JvmArguments = argParse(jvmArg);
                }

                // metadata
                version.ReleaseTime = job["releaseTime"]?.ToString();

                var type = job["type"]?.ToString();
                version.TypeStr = type;
                version.Type = ProfileConverter.FromString(type);

                // inherits
                if (job["inheritsFrom"] != null)
                {
                    version.IsInherited = true;
                    version.ParentVersionId = job["inheritsFrom"]?.ToString();
                }

                version.Jar = job["jar"]?.ToString();
                if (string.IsNullOrEmpty(version.Jar))
                    version.Jar = version.id;

                // logging
                var loggingClient = job["logging"]?["client"];
                if (loggingClient != null)
                {
                    version.LoggingClient = new src.LogConfig
                    {
                        Id = loggingClient["file"]?["id"]?.ToString(),
                        Sha1 = loggingClient["file"]?["sha1"]?.ToString(),
                        Size = loggingClient["file"]?["size"]?.ToString(),
                        Url = loggingClient["file"]?["url"]?.ToString(),
                        Type = loggingClient["type"]?.ToString(),
                        Argument = loggingClient["argument"]?.ToString()
                    };
                }

                return version;
            }
            catch (ParseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ParseException(ex);
            }
        }

        private static string[] argParse(JArray arr)
        {
            var strList = new List<string>(arr.Count);

            foreach (var item in arr)
            {
                if (item is JObject)
                {
                    bool allow = true;

                    JArray rules = item["rules"] as JArray ?? item["compatibilityRules"] as JArray;
                    if (rules != null)
                        allow = Rule.CheckOSRequire(rules);

                    var value = item["value"] ?? item["values"];

                    if (allow && value != null)
                    {
                        if (value is JArray)
                        {
                            foreach (var str in value)
                            {
                                strList.Add(str.ToString());
                            }
                        }
                        else
                            strList.Add(value.ToString());
                    }
                }
                else
                    strList.Add(item.ToString());
            }

            return strList.ToArray();
        }
    }
}
