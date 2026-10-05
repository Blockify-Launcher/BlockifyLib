using BlockifyLib.Launcher.Minecraft;
using BlockifyLib.Launcher.Minecraft.Mojang;
using BlockifyLib.Launcher.Utils;
using BlockifyLib.Launcher.Version.Metadata;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;

namespace BlockifyLib.Launcher.Version.Load
{
    // Mojang version manifest (v2). Network first with a short timeout, then the disk cache
    // in <minecraft>/versions/version_manifest_v2.json. Throws only when both are unavailable.
    public class MojangLoader : IVersionLoader
    {
        public const string CacheFileName = "version_manifest_v2.json";

        // Timeout of the manifest request.
        public static TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(8);

        // A fetched manifest is reused in memory for this long, so one start doesn't download it twice.
        public static TimeSpan MemoryCacheTime { get; set; } = TimeSpan.FromMinutes(10);

        private static readonly object memLock = new object();
        private static string? memJson;
        private static DateTime memTime;
        private static Task<(string json, DateTime fetchedAt)>? inflight;

        private readonly MinecraftPath? cachePath;

        public MojangLoader() { }

        // cachePath: where to keep the manifest copy for offline starts (null = no disk cache).
        public MojangLoader(MinecraftPath? cachePath) => this.cachePath = cachePath;

        public string? CacheFilePath =>
            cachePath == null ? null : Path.Combine(cachePath.Versions, CacheFileName);

        public VersionCollection GetVersionMetadatas() =>
            Task.Run(() => GetVersionMetadatasAsync()).GetAwaiter().GetResult();

        public async Task<VersionCollection> GetVersionMetadatasAsync()
        {
            try
            {
                var (json, fetchedAt) = await fetchAsync().ConfigureAwait(false);
                var live = parseList(json);
                live.Source = VersionSource.Live;
                saveCache(json, fetchedAt);
                return live;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                string? cached = readCache();
                if (cached == null)
                    throw;

                var fromCache = parseList(cached);
                fromCache.Source = VersionSource.Cache;
                return fromCache;
            }
        }

        // Clears the in-memory copy (e.g. user pressed "refresh").
        public static void ResetMemoryCache()
        {
            lock (memLock)
            {
                memJson = null;
                inflight = null;
            }
        }

        private static Task<(string json, DateTime fetchedAt)> fetchAsync()
        {
            lock (memLock)
            {
                if (memJson != null && DateTime.UtcNow - memTime < MemoryCacheTime)
                    return Task.FromResult<(string json, DateTime fetchedAt)>((memJson, memTime));

                // Parallel callers share one request.
                if (inflight == null || inflight.IsCompleted)
                    inflight = downloadManifestAsync();
                return inflight;
            }
        }

        private static async Task<(string json, DateTime fetchedAt)> downloadManifestAsync()
        {
            string json = await LibHttp.GetStringAsync(MojangServer.VersionManifestV2, RequestTimeout)
                .ConfigureAwait(false);

            // A captive portal page is not a manifest: never cache it.
            if (JObject.Parse(json)["versions"] is not JArray)
                throw new InvalidDataException("Invalid version manifest");

            DateTime now = DateTime.UtcNow;
            lock (memLock)
            {
                memJson = json;
                memTime = now;
            }
            return (json, now);
        }

        private string? readCache()
        {
            try
            {
                string? file = CacheFilePath;
                if (file == null || !File.Exists(file))
                    return null;
                return File.ReadAllText(file);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        // Atomic write: temp file + move, skipped when the cache is already this fresh.
        private void saveCache(string json, DateTime fetchedAt)
        {
            string? file = CacheFilePath;
            if (file == null)
                return;

            string tmp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                if (File.Exists(file) && File.GetLastWriteTimeUtc(file) >= fetchedAt)
                    return;

                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(tmp, json);
                File.Move(tmp, file, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                try
                {
                    if (File.Exists(tmp))
                        File.Delete(tmp);
                }
                catch { }
            }
        }

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
