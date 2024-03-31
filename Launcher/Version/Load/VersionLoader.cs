using BlockifyLib.Launcher.Minecraft;
using BlockifyLib.Launcher.Version.Metadata;
using System.IO;

namespace BlockifyLib.Launcher.Version.Load
{
    public interface IVersionLoader
    {
        Task<VersionCollection> GetVersionMetadatasAsync();
        VersionCollection GetVersionMetadatas();
    }

    public class LocalVersionLoader : IVersionLoader
    {
        public LocalVersionLoader(MinecraftPath path)
        {
            minecraftPath = path;
        }

        private readonly MinecraftPath minecraftPath;

        public VersionCollection GetVersionMetadatas()
        {
            var list = getFromLocal(minecraftPath).ToArray();
            return new VersionCollection(list, minecraftPath);
        }

        public Task<VersionCollection> GetVersionMetadatasAsync()
        {
            return Task.FromResult(GetVersionMetadatas());
        }

        private List<VersionMetadata> getFromLocal(MinecraftPath path)
        {
            var versionDirectory = new DirectoryInfo(path.Versions);
            if (!versionDirectory.Exists)
                return new List<VersionMetadata>();

            var dirs = versionDirectory.GetDirectories();
            var arr = new List<VersionMetadata>(dirs.Length);

            for (int i = 0; i < dirs.Length; i++)
            {
                var dir = dirs[i];
                var filepath = Path.Combine(dir.FullName, dir.Name + ".json");
                if (File.Exists(filepath))
                {
                    LocalVersion info = new LocalVersion(dir.Name)
                    {
                        Path = filepath,
                        Type = "local",
                        ProfType = ProfileConverter.VersionType.Custom
                    };
                    arr.Add(info);
                }
            }

            return arr;
        }
    }

    public class DefaultVersionLoader : IVersionLoader
    {
        public DefaultVersionLoader(MinecraftPath path) =>
            MinecraftPath = path;

        protected MinecraftPath MinecraftPath;

        public VersionCollection GetVersionMetadatas()
        {
            LocalVersionLoader localVersionLoader = new LocalVersionLoader(MinecraftPath);
            MojangLoader mojangVersionLoader = new MojangLoader();

            VersionCollection localVersions = localVersionLoader.GetVersionMetadatas();
            localVersions.Merge(mojangVersionLoader.GetVersionMetadatas());
            return localVersions;
        }

        public async Task<VersionCollection> GetVersionMetadatasAsync()
        {
            LocalVersionLoader localVersionLoader = new LocalVersionLoader(MinecraftPath);
            MojangLoader mojangVersionLoader = new MojangLoader();

            VersionCollection localVersions = await localVersionLoader.GetVersionMetadatasAsync()
                .ConfigureAwait(false);
            localVersions.Merge(await mojangVersionLoader.GetVersionMetadatasAsync()
                .ConfigureAwait(false));
            return localVersions;
        }
    }
}
