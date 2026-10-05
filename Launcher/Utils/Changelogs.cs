using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BlockifyLib.Launcher.Utils
{
    public class Changelogs
    {
        private static readonly Dictionary<string, string> changelogUrls = new Dictionary<string, string>
        {
            { "1.14.2", "https://feedback.minecraft.net/hc/en-us/articles/360028919851-Minecraft-Java-Edition-1-14-2" },
            { "1.14.3", "https://feedback.minecraft.net/hc/en-us/articles/360030771451-Minecraft-Java-Edition-1-14-3" },
            { "1.14.4", "https://feedback.minecraft.net/hc/en-us/articles/360030780172-Minecraft-Java-Edition-1-14-4" },
        };

        public static async Task<Changelogs> GetChangelogs()
        {
            var url = "https://launchercontent.mojang.com/javaPatchNotes.json";
            string response = await LibHttp.GetStringAsync(url).ConfigureAwait(false);

            var obj = JObject.Parse(response);
            var versionDict = new Dictionary<string, string?>();
            var array = obj["entries"] as JArray;
            if (array != null)
            {
                foreach (var item in array)
                {
                    var version = item["version"]?.ToString();
                    if (string.IsNullOrEmpty(version))
                        continue;

                    var body = item["body"]?.ToString();
                    versionDict[version] = body;
                }
            }

            return new Changelogs(versionDict);
        }

        private Changelogs(Dictionary<string, string?> versions)
        {
            this.versions = versions;
        }

        private readonly Dictionary<string, string?> versions;

        public string[] GetAvailableVersions()
        {
            var list = new List<string>();
            list.AddRange(versions.Keys);

            foreach (var item in changelogUrls)
            {
                if (!versions.ContainsKey(item.Key))
                    list.Add(item.Key);
            }

            return list.ToArray();
        }

        public async Task<string?> GetChangelogHtml(string version)
        {
            if (versions.TryGetValue(version, out string? body))
                return body;
            if (changelogUrls.TryGetValue(version, out string? url))
                return await GetChangelogFromUrl(url).ConfigureAwait(false);

            return null;
        }

        private static readonly Regex articleRegex = new Regex(
            "<article class=\\\"article\\\">(.*)<\\/article>", RegexOptions.Singleline);

        private async Task<string> GetChangelogFromUrl(string url)
        {
            string html = await LibHttp.GetStringAsync(url).ConfigureAwait(false);

            var regResult = articleRegex.Match(html);
            if (!regResult.Success)
                return "";

            return regResult.Value;
        }
    }
}
