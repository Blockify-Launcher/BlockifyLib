using Newtonsoft.Json.Linq;
using System.IO;
using static BlockifyLib.Launcher.Library;

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

        private static Version ParseFromJson(string json, bool writeProfile = true)
        {
            try
            {
                JObject job = JObject.Parse(json);

                if (string.IsNullOrEmpty(GetObject(job, "id")))
                    throw new ParseException("Empty version id");
                Version version = new Version(GetObject(job, "id"));
                version.JavaVersion = job["javaVersion"]?["component"]?.ToString();

                if (job["assetIndex"] != null)
                {
                    version.AssetId = GetObject((JObject)job["assetIndex"], "id");
                    version.AssetUrl = GetObject((JObject)job["assetIndex"], "url");
                    version.AssetHash = GetObject((JObject)job["assetIndex"], "shal");
                }
                else if (job["assets"] != null)
                    version.AssetId = job["assets"].ToString();

                if (job["downloads"]?["client"] != null)
                {
                    version.ClientDownloadUrl = GetObject((JObject)job["downloads"]?["client"], "url");
                    version.ClientHash = GetObject((JObject)job["downloads"]?["client"], "shal");
                }

                if (job["libraries"] is JArray libJArr)
                {
                    List<Library> libList = new List<Library>(libJArr.Count);
                    Parser libParser = new Parser();
                    foreach (JObject item in libJArr)
                    {
                        var libs = Library.Parser.ParseJson(item);
                        if (libs != null)
                            libList.AddRange(libs);
                    }

                    version.Libraries = libList.ToArray();
                }

                version.MainClass = job["mainClass"]?.ToString();
                version.MinecraftArguments = job["minecraftArguments"]?.ToString();

                var ag = job["arguments"];
                if (ag != null)
                {
                    if (ag["game"] is JArray gameArg)
                        version.GameArguments = argParse(gameArg);
                    if (ag["jvm"] is JArray jvmArg)
                        version.JvmArguments = argParse(jvmArg);
                }

                version.ReleaseTime = job["releaseTime"]?.ToString();

                var type = job["type"]?.ToString();
                version.TypeStr = type;
                version.Type = ProfileConverter.FromString(type);

                if (job["inheritsFrom"] != null)
                {
                    version.IsInherited = true;
                    version.ParentVersionId = job["inheritsFrom"]?.ToString();
                }

                version.Jar = job["jar"]?.ToString();
                if (string.IsNullOrEmpty(version.Jar))
                    version.Jar = version.id;

                var loggingClient = job["logging"]?["client"];
                if (loggingClient != null)
                {
                    version.LoggingClient = new src.LogConfig
                    {
                        Id = loggingClient["file"]?["id"]?.ToString(),
                        Shal = loggingClient["file"]?["sha1"]?.ToString(),
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
