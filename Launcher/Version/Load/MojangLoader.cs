using BlockifyLib.Launcher.Minecraft.Mojang;
using BlockifyLib.Launcher.Version.Metadata;
using Newtonsoft.Json.Linq;
using System.Net;

namespace BlockifyLib.Launcher.Version.Load
{
    public class MojangLoader : IVersionLoader
    {
        public VersionCollection GetVersionMetadatas() =>
            parseList(new WebClient().DownloadString(MojangServer.Version));

        public async Task<VersionCollection> GetVersionMetadatasAsync() =>
            parseList(await new WebClient().DownloadStringTaskAsync(MojangServer.Version));

        private VersionCollection parseList(string res)
        {
            string? latestReleaseId = null;
            string? latestSnapshotId = null;

            VersionMetadata? latestRelease = null;
            VersionMetadata? latestSnapshot = null;

            var jobj = JObject.Parse(res);
            var jarr = jobj["versions"] as JArray;

            var latest = jobj["latest"];
            if (latest != null)
            {
                latestReleaseId = latest["release"]?.ToString();
                latestSnapshotId = latest["snapshot"]?.ToString();
            }

            bool checkLatestRelease = !string.IsNullOrEmpty(latestReleaseId);
            bool checkLatestSnapshot = !string.IsNullOrEmpty(latestSnapshotId);

            var arr = new List<WebVersion>(jarr?.Count ?? 0);
            if (jarr != null)
            {
                foreach (var t in jarr)
                {
                    WebVersion? obj = t.ToObject<WebVersion>();
                    if (obj == null)
                        continue;

                    obj.ProfType = ProfileConverter.FromString(obj.Type);
                    arr.Add(obj);

                    if (checkLatestRelease && obj.Name == latestReleaseId)
                        latestRelease = obj;
                    if (checkLatestSnapshot && obj.Name == latestSnapshotId)
                        latestSnapshot = obj;
                }
            }

            return new VersionCollection(arr.ToArray(), null, latestRelease, latestSnapshot);
        }
    }
}

