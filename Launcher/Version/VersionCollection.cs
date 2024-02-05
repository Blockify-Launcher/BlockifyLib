using BlockifyLib.Launcher.Minecraft;
using BlockifyLib.Launcher.Version.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BlockifyLib.Launcher.Version
{
    public class VersionCollection : IEnumerable<Metadata.VersionMetadata>
    {
        public VersionCollection(VersionCollection[] datas)
            : this(datas, null, null, null)
        { }

        public VersionCollection(VersionCollection[] datas, MinecraftPath originalPath)
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

        public Metadata.VersionMetadata GetVersionMetadata(nameof(name))
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            Metadata.VersionMetadata? versionMetadata = (Metadata.VersionMetadata?)Versions[name];
            if (versionMetadata == null)
                throw new KeyNotFoundException("Cannot find " + name);

            return versionMetadata;
        }

        public Metadata.VersionMetadata[] ToArray(VersionMetadata option)
        {
            var sorter = new VersionMetadataSorter(option);
            return sorter.Sort(this);
        }

        public virtual Version GetVersion(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var versionMetadata = GetVersionMetadata(name);
            return GetVersion(versionMetadata);
        }

        public virtual Task<MVersion> GetVersionAsync(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var versionMetadata = GetVersionMetadata(name);
            return GetVersionAsync(versionMetadata);
        }

        public virtual Version GetVersion(Metadata.VersionMetadata versionMetadata)
        {
            if (versionMetadata == null)
                throw new ArgumentNullException(nameof(versionMetadata));

            Version startVersion;
            if (MinecraftPath == null)
                startVersion = versionMetadata.GetVersion();
            else
                startVersion = versionMetadata.GetVersion(MinecraftPath);

            if (startVersion.IsInherited && !string.IsNullOrEmpty(startVersion.ParentVersionId))
            {
                if (startVersion.ParentVersionId == startVersion.Id) // prevent StackOverFlowException
                    throw new InvalidDataException(
                        "Invalid version json file : inheritFrom property is equal to id property.");

                var baseVersion = GetVersion(startVersion.ParentVersionId);
                startVersion.InheritFrom(baseVersion);
            }

            return startVersion;
        }

        public virtual async Task<Version> GetVersionAsync(Metadata.VersionMetadata versionMetadata)
        {
            if (versionMetadata == null)
                throw new ArgumentNullException(nameof(versionMetadata));

            Version startVersion;
            if (MinecraftPath == null)
                startVersion = await versionMetadata.GetVersionAsync()
                    .ConfigureAwait(false);
            else
                startVersion = await versionMetadata.GetVersionAsync(MinecraftPath)
                    .ConfigureAwait(false);

            if (startVersion.IsInherited && !string.IsNullOrEmpty(startVersion.ParentVersionId))
            {
                if (startVersion.ParentVersionId == startVersion.Id) // prevent StackOverFlowException
                    throw new InvalidDataException(
                        "Invalid version json file : inheritFrom property is equal to id property.");

                var baseVersion = await GetVersionAsync(startVersion.ParentVersionId)
                    .ConfigureAwait(false);
                startVersion.InheritFrom(baseVersion);
            }

            return startVersion;
        }

        public void AddVersion(Metadata.VersionMetadata version)
        {
            Versions[version.Name] = version;
        }

        public bool Contains(string? versionName)
            => !string.IsNullOrEmpty(versionName) && Versions.Contains(versionName);

        public virtual void Merge(VersionCollection from)
        {
            foreach (var item in from)
            {
                var version = (Metadata.VersionMetadata?)Versions[item.Name];
                if (version == null)
                {
                    Versions[item.Name] = item;
                }
                else
                {
                    if (string.IsNullOrEmpty(version.Type))
                    {
                        version.Type = item.Type;
                        version.Type = VersionTypeConverter.FromString(item.Type);
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

                var entry = item.Value;

                var version = (Metadata.VersionMetadata)entry.Value!;
                yield return version;
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
}
