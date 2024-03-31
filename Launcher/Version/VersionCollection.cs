using BlockifyLib.Launcher.Minecraft;
using System.Collections;
using System.Collections.Specialized;
using System.IO;

namespace BlockifyLib.Launcher.Version
{
    public class VersionCollection : IEnumerable<Metadata.VersionMetadata>
    {
        public VersionCollection(Metadata.VersionMetadata[] datas)
            : this(datas, null, null, null)
        { }

        public VersionCollection(Metadata.VersionMetadata[] datas, MinecraftPath originalPath)
            : this(datas, originalPath, null, null)
        { }

        public Metadata.VersionMetadata? LatestReleaseVersion { get; private set; }
        public Metadata.VersionMetadata? LatestSnapshotVersion { get; private set; }
        public MinecraftPath? MinecraftPath { get; private set; }
        protected OrderedDictionary Versions;

        public VersionCollection(
            Metadata.VersionMetadata[] datas,
            MinecraftPath? originalPath,
            Metadata.VersionMetadata? latestRelease,
            Metadata.VersionMetadata? latestSnapshot)
        {
            if (datas == null)
                throw new ArgumentNullException(nameof(datas));

            Versions = new OrderedDictionary();
            foreach (var item in datas)
            {
                Versions.Add(item.Name, item);
            }

            MinecraftPath = originalPath;
            LatestReleaseVersion = latestRelease;
            LatestSnapshotVersion = latestSnapshot;
        }

        public Metadata.VersionMetadata this[int index] =>
            (Metadata.VersionMetadata)Versions[index]!;

        public Metadata.VersionMetadata GetVersionMetadata(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            Metadata.VersionMetadata? versionMetadata = (Metadata.VersionMetadata?)Versions[name];
            if (versionMetadata == null)
                throw new KeyNotFoundException("Cannot find " + name);

            return versionMetadata;
        }

        public Metadata.VersionMetadata[] ToArray(Func.SortVersionOption option) =>
            new Func.SortVersionMetadata(option).Sort(this);

        public virtual Version GetVersion(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            return GetVersion(GetVersionMetadata(name));
        }

        public virtual Task<Version> GetVersionAsync(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            return GetVersionAsync(GetVersionMetadata(name));
        }

        public virtual Version GetVersion(Metadata.VersionMetadata versionMetadata)
        {
            if (versionMetadata == null)
                throw new ArgumentNullException(nameof(versionMetadata));

            Version startVersion = MinecraftPath == null ? versionMetadata.GetVersion() :
                versionMetadata.GetVersion(MinecraftPath);

            if (startVersion.IsInherited && !string.IsNullOrEmpty(startVersion.ParentVersionId))
            {
                if (startVersion.ParentVersionId == startVersion.id)
                    throw new InvalidDataException("Invalid version json file");
                startVersion.InheritFrom(GetVersion(startVersion.ParentVersionId));
            }
            return startVersion;
        }

        public virtual async Task<Version> GetVersionAsync(Metadata.VersionMetadata versionMetadata)
        {
            if (versionMetadata == null)
                throw new ArgumentNullException(nameof(versionMetadata));

            Version startVersion = MinecraftPath == null ? await versionMetadata.GetVersionAsync().ConfigureAwait(false) :
                await versionMetadata.GetVersionAsync(MinecraftPath).ConfigureAwait(false);

            if (startVersion.IsInherited && !string.IsNullOrEmpty(startVersion.ParentVersionId))
            {
                if (startVersion.ParentVersionId == startVersion.id)
                    throw new InvalidDataException("Invalid version json file");
                startVersion.InheritFrom(await GetVersionAsync(startVersion.ParentVersionId)
                    .ConfigureAwait(false));
            }
            return startVersion;
        }

        public void AddVersion(Metadata.VersionMetadata version) =>
            Versions[version.Name] = version;

        public bool Contains(string? versionName) =>
            !string.IsNullOrEmpty(versionName) && Versions.Contains(versionName);

        public virtual void Merge(VersionCollection from)
        {
            foreach (var item in from)
            {
                Metadata.VersionMetadata? version = Versions[item.Name] as Metadata.VersionMetadata;
                if (version == null)
                    Versions[item.Name] = item;
                else
                {
                    if (string.IsNullOrEmpty(version.Type))
                    {
                        version.Type = item.Type;
                        version.ProfType = ProfileConverter.FromString(item.Type);
                    }

                    if (string.IsNullOrEmpty(version.ReleaseTimeStr))
                        version.ReleaseTimeStr = item.ReleaseTimeStr;
                }
            }

            if (this.MinecraftPath == null && from.MinecraftPath != null)
                this.MinecraftPath = from.MinecraftPath;

            if (this.LatestReleaseVersion == null && from.LatestReleaseVersion != null)
                this.LatestReleaseVersion = from.LatestReleaseVersion;

            if (this.LatestSnapshotVersion == null && from.LatestSnapshotVersion != null)
                this.LatestSnapshotVersion = from.LatestSnapshotVersion;
        }

        public IEnumerator<Metadata.VersionMetadata> GetEnumerator()
        {
            foreach (DictionaryEntry? item in Versions)
            {
                if (!item.HasValue)
                    continue;
                yield return (Metadata.VersionMetadata)item.Value.Value!;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (DictionaryEntry? item in Versions)
            {
                if (!item.HasValue)
                    continue;
                yield return item.Value;
            }
        }
    }
}
