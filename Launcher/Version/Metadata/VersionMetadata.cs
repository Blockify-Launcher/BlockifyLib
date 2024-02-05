using BlockifyLib.Launcher.Version.Func;
using BlockifyLib.Launcher.Minecraft;
using BlockifyLib.Launcher.Utils;
using Newtonsoft.Json;
using System.IO;

namespace BlockifyLib.Launcher.Version.Metadata
{
    public abstract class VersionMetadata
    {
        [JsonProperty("id")] public string Name { get; private set; }
        [JsonProperty("type")] public string? Type { get; set; }
        [JsonProperty("releaseTime")] public string? ReleaseTimeStr { get; set; }
        [JsonProperty("url")] public string? Path { get; set; }

        public bool IsLocalVersion { get; set; }

        protected VersionMetadata(string id) =>
            this.Name = id;

        public ProfileConverter.ProfileType ProfType { get; set; }

        public DateTime? ReleaseTime
        {
            get =>
                DateTime.TryParse(this.ReleaseTimeStr, out DateTime dt) ? dt : null;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;
            if (((VersionMetadata)obj)?.Name != null)
                return ((VersionMetadata)obj).Name.Equals(Name);
            if (obj is string)
                return Name.Equals(obj.ToString());

            return false;
        }

        public override string ToString() =>
            Type + " " + Name;

        public override int GetHashCode() =>
            Name.GetHashCode();

        public abstract Version GetVersion();
        public abstract Version GetVersion(MinecraftPath savePath);
        public abstract Task<Version> GetVersionAsync();
        public abstract Task<Version> GetVersionAsync(MinecraftPath savePath);
        public abstract void Save(MinecraftPath path);
        public abstract Task SaveAsync(MinecraftPath path);
    }

    public abstract class StrMeta : VersionMetadata
    {
        protected StrMeta(string id) : base(id) { }

        protected abstract string ReadMetadata();
        protected abstract Task<string> ReadMetadataAsync();

        private string? prepareSaveMetadata(MinecraftPath path)
        {
            if (string.IsNullOrEmpty(Name))
                return null;

            if (IsLocalVersion && !string.IsNullOrEmpty(Path))
            {
                var result = string.Compare(IOUtil.NormalizePath(Path), path.GetVersionJsonPath(Name),
                    StringComparison.InvariantCultureIgnoreCase);

                if (result == 0)
                    return null;
            }

            string directoryPath = System.IO.Path.GetDirectoryName(path.GetVersionJsonPath(Name));
            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            return path.GetVersionJsonPath(Name);
        }

        protected virtual void SaveMetadata(string metadata, MinecraftPath path)
        {
            var metadataPath = prepareSaveMetadata(path);
            if (!string.IsNullOrEmpty(metadataPath))
                File.WriteAllText(metadataPath, metadata);
        }

        protected virtual Task SaveMetadataAsync(string metadata, MinecraftPath path)
        {
            var metadataPath = prepareSaveMetadata(path);
            if (!string.IsNullOrEmpty(metadataPath))
                return IOUtil.WriteFileAsync(metadataPath, metadata);
            else
                return Task.CompletedTask;
        }

        private async Task<Version> getAsync(MinecraftPath? savePath, bool parse, bool sync)
        {
            string Json = sync ? ReadMetadata() : await ReadMetadataAsync().ConfigureAwait(false);
            if (savePath != null)
                if (sync)
                    SaveMetadata(Json, savePath);
                else
                    await SaveMetadataAsync(Json, savePath)
                        .ConfigureAwait(false);
            return parse ? Parser.ParseFromJson(Json) : null;// TODO : create Version Parser
        }

        public override Version GetVersion()
            => getAsync(null, true, true).GetAwaiter().GetResult()!;

        public override Version GetVersion(MinecraftPath savePath)
            => getAsync(savePath, true, true).GetAwaiter().GetResult()!;

        public override Task<Version> GetVersionAsync()
            => getAsync(null, true, false)!;

        public override Task<Version> GetVersionAsync(MinecraftPath savePath)
            => getAsync(savePath, true, false)!;

        public override void Save(MinecraftPath path)
            => getAsync(path, false, true).GetAwaiter().GetResult();

        public override Task SaveAsync(MinecraftPath path)
            => getAsync(path, false, false);
    }

}
