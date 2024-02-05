using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;

namespace BlockifyLib.Launcher
{
    public partial class ProfileInfo
    {
        public bool IsWeb = true;

        [JsonProperty("id")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("releaseTime")]
        public string ReleaseTime { get; set; }

        [JsonProperty("url")]
        public string Path { get; set; }

        public override bool Equals(object? obj)
        {
            ProfileInfo? other = obj as ProfileInfo;
            if (other != null)
                return other.Name.Equals(Name);
            else if (obj is string)
                return other.Name.Equals(obj.ToString());
            else
                return base.Equals(obj);
        }

        public override string ToString() =>
            Type + " " + Name;

        public override int GetHashCode() =>
            Name.GetHashCode();

        public static ProfileInfo[] GetProfiles()
        {
            HashSet<ProfileInfo> list = new HashSet<ProfileInfo>(GetProfilesFromLocal());
            foreach (var item in GetProfilesFromWeb())
            {
                bool isexist = false;
                foreach (var local in list)
                    if (local.Name == item.Name)
                    {
                        isexist = true;
                        break;
                    }
                if (!isexist)
                    list.Add(item);
            }

            return list.ToArray();
        }

        public static ProfileInfo[] GetProfilesFromLocal()
        {
            DirectoryInfo[] dirs = new DirectoryInfo(Minecraft.Minecraft.Versions).GetDirectories();
            List<ProfileInfo> list = new List<ProfileInfo>(dirs.Length);

            for (int i = 0; i < dirs.Length; i++)
            {
                var dir = dirs[i];
                var filepath = dir.FullName + @"\" + dir.Name + ".json";
                if (File.Exists(filepath))
                {
                    ProfileInfo info = new ProfileInfo();
                    info.IsWeb = false;
                    info.Name = dir.Name;
                    info.Path = filepath;
                    list.Add(info);
                }
            }
            return list.ToArray();
        }


        // TODO : version split.
        public static ProfileInfo[] GetProfilesFromWeb()
        {
            JArray jArray;

            using (WebClient webCli = new WebClient())
            {
                var jobj = JObject.Parse(
                    webCli.DownloadString("https://launchermeta.mojang.com/mc/game/version_manifest.json")
                    );
                jArray = JArray.Parse(jobj.ToString());
            }

            ProfileInfo[] arrProfile = new ProfileInfo[jArray.Count];
            for (int i = 0; i < jArray.Count; i++)
            {
                var obj = jArray[i].ToObject<ProfileInfo>();
                obj.IsWeb = true;
                arrProfile[i] = obj;
            }
            return arrProfile;
        }
    }

    public class Version
    {
        [Obsolete("Use 'FindProfile' Method.")]
        public static Version GetProfile(ProfileInfo[] infos, string name) =>
            FindProfile(infos, name);

        static string n(string t) => t == null ? "" : t;

        static bool nc(string t) => t == null || t == "";

        public static Version Parse(ProfileInfo info)
        {
            string json;
            if (info.IsWeb)
                using (WebClient Web = new WebClient())
                    return ParseFromJson(Web.DownloadString(info.Path), true);
            else
                return ParseFromFile(info.Path);
        }

        public static Version ParseFromFile(string path) =>
            ParseFromJson(File.ReadAllText(path), false);

        private static string GetObject(JObject obj, string name) =>
            obj[name]?.ToString();

        static string[] argParse(JArray arr)
        {
            List<string> strList = new List<string>(arr.Count);
            Rule ruleChecker = new Rule();

            foreach (var item in arr)
                if (item is JObject)
                {
                    bool allow = true;

                    if (item["rules"] != null)
                        allow = ruleChecker.CheckOSRequire((JArray)item["rules"]);

                    JToken value = item["value"] ?? item["values"];

                    if (allow && value != null)
                        if (value is JArray)
                            foreach (var str in value)
                                strList.Add(str.ToString());
                        else
                            strList.Add(value.ToString());
                }
                else
                    strList.Add(item.ToString());

            return strList.ToArray();
        }

        private static Version ParseFromJson(string json, bool writeProfile = true)
        {
            Version profile = new Version();
            JObject job = JObject.Parse(json);
            profile.Id = GetObject(job, "id");


            JObject asset = job["assetIndex"] as JObject;
            if (asset != null)
            {
                profile.AssetId = n(GetObject(asset, "id"));
                profile.AssetUrl = n(GetObject(asset, "url"));
                profile.AssetHash = n(GetObject(asset, "sha1"));
            }

            JObject client = job["downloads"]?["client"] as JObject;
            if (client != null)
            {
                profile.ClientDownloadUrl = GetObject(client, "url");
                profile.ClientHash = GetObject(client, "sha1");
            }

            profile.Libraries = Library.Parser.ParseJson(
                (JArray)job["libraries"]);

            profile.MainClass = n(GetObject(job, "mainClass"));
            string MinecraftArgm = GetObject(job, "minecraftArguments");

            if (MinecraftArgm != null)
                profile.MinecraftArguments = MinecraftArgm;

            JObject arguments = job["arguments"] as JObject;
            if (arguments != null)
            {
                if (arguments["game"] != null)
                    profile.GameArguments = argParse((JArray)arguments["game"]);
                if (arguments["jvm"] != null)
                    profile.JvmArguments = argParse((JArray)arguments["jvm"]);
            }

            profile.ReleaseTime = GetObject(job, "releaseTime");
            string type = GetObject(job, "type");

            profile.TypeStr = type;
            profile.Type = ProfileConverter.FromString(type);

            if (job["inheritsFrom"] != null)
            {
                profile.IsInherted = true;
                profile.ParentProfileId = job["inheritsFrom"].ToString();
            }
            else
                profile.Jar = profile.Id;

            if (writeProfile)
            {
                string path = Minecraft.Minecraft.Versions + profile.Id;
                Directory.CreateDirectory(path);
                File.WriteAllText(path + "\\" + profile.Id + ".json", json);
            }

            return profile;
        }

        private static Version inhert(Version parent, Version child)
        {
            if (nc(child.AssetId))
                child.AssetId = parent.AssetId;

            if (nc(child.AssetUrl))
                child.AssetUrl = parent.AssetUrl;

            if (nc(child.AssetHash))
                child.AssetHash = parent.AssetHash;

            if (nc(child.ClientDownloadUrl))
                child.ClientDownloadUrl = parent.ClientDownloadUrl;

            if (nc(child.ClientHash))
                child.ClientHash = parent.ClientHash;

            if (nc(child.MainClass))
                child.MainClass = parent.MainClass;

            if (nc(child.MinecraftArguments))
                child.MinecraftArguments = parent.MinecraftArguments;

            child.Jar = parent.Jar;

            if (parent.Libraries != null)
            {
                if (child.Libraries != null)
                    child.Libraries = child.Libraries.Concat(parent.Libraries).ToArray();
                else
                    child.Libraries = parent.Libraries;
            }

            if (parent.GameArguments != null)
            {
                if (child.GameArguments != null)
                    child.GameArguments = child.GameArguments.Concat(parent.GameArguments).ToArray();
                else
                    child.GameArguments = parent.GameArguments;
            }


            if (parent.JvmArguments != null)
            {
                if (child.JvmArguments != null)
                    child.JvmArguments = child.JvmArguments.Concat(parent.JvmArguments).ToArray();
                else
                    child.JvmArguments = parent.JvmArguments;
            }

            return child;
        }

        public static Version FindProfile(ProfileInfo[] infos, string name)
        {
            Version startProfile = null;
            Version baseProfile = null;

            foreach (ProfileInfo itemInfo in infos)
                if (itemInfo.Name == name)
                {
                    //startProfile =
                    break;
                }

            if (startProfile.IsInherted)
            {
                baseProfile = FindProfile(infos, startProfile.ParentProfileId);
                inhert(baseProfile, startProfile);
            }

            return startProfile;
        }

        public bool IsWeb { get; private set; }

        public bool IsInherted { get; private set; } = false;
        public string ParentProfileId { get; private set; } = "";

        public string Id { get; private set; } = "";

        public string AssetId { get; private set; } = "";
        public string AssetUrl { get; private set; } = "";
        public string AssetHash { get; private set; } = "";

        public string Jar { get; private set; } = "";
        public string ClientDownloadUrl { get; private set; } = "";
        public string ClientHash { get; private set; } = "";

        public Library[] Libraries { get; private set; }
        public string MainClass { get; private set; } = "";
        public string MinecraftArguments { get; private set; } = "";

        public string[] GameArguments { get; private set; }
        public string[] JvmArguments { get; private set; }
        public string ReleaseTime { get; private set; } = "";

        public ProfileConverter.ProfileType Type { get; private set; } = ProfileConverter.ProfileType.Custom;

        public string TypeStr { get; private set; } = "";
        public string NativePath { get; set; } = "";
    }
}
