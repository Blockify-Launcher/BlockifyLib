using BlockifyLib.Launcher.Minecraft;
using BlockifyLib.Launcher.Utils;
using System.Diagnostics;
using System.IO;

namespace BlockifyLib.Launcher.src
{
    public class Native
    {
        public Native(MinecraftPath gamePath, Version.Version version)
        {
            this.version = version;
            this.gamePath = gamePath;
        }

        private readonly Version.Version version;
        private readonly MinecraftPath gamePath;

        public string ExtractNatives()
        {
            string path = gamePath.GetNativePath(version.id);
            Directory.CreateDirectory(path);

            if (version.Libraries == null) return path;

            foreach (var item in version.Libraries)
                if (item.IsRequire && item.IsNative && !string.IsNullOrEmpty(item.Path))
                {
                    string zPath = Path.Combine(gamePath.Library, item.Path);
                    if (File.Exists(zPath))
                    {
                        var z = new SharpZip(zPath);
                        z.Unzip(path);
                    }
                }

            return path;
        }

        public void CleanNatives()
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(
                    gamePath.GetNativePath(version.id));
                if (!di.Exists)
                    return;

                foreach (var item in di.GetFiles())
                    item.Delete();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }
}
