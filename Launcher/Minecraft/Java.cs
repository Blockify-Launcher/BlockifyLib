using BlockifyLib.Launcher.Downloader;
using BlockifyLib.Launcher.Minecraft.Mojang;
using BlockifyLib.Launcher.src;
using BlockifyLib.Launcher.Utils;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace BlockifyLib.Launcher.Minecraft
{
    public class Java
    {
        public static readonly string DefaultRuntimeDirectory
           = Path.Combine(MinecraftPath.GetOSDefaultPath(), "runtime");

        public event ProgressChangedEventHandler? ProgressChanged;
        public string RuntimeDirectory { get; private set; }

        private IProgress<ProgressChangedEventArgs>? pProgressChanged;

        public IJavaPathResolver JavaPathResolver { get; set; }

        public Java() : this(DefaultRuntimeDirectory) { }

        public Java(string runtimePath)
        {
            RuntimeDirectory = runtimePath;
            JavaPathResolver = new MinecraftJavaPathResolver(runtimePath);
        }

        public string GetBinaryPath()
            => JavaPathResolver.GetJavaBinaryPath(MinecraftJavaPathResolver.CmlLegacyVersionName, Rule.OSName);

        public bool CheckJavaExistence()
            => File.Exists(GetBinaryPath());

        public string CheckJava()
        {
            pProgressChanged = new Progress<ProgressChangedEventArgs>(
                (e) => ProgressChanged?.Invoke(this, e));

            string javaPath = GetBinaryPath();

            if (!CheckJavaExistence())
            {
                string javaUrl = GetJavaUrl();
                string lzmaPath = downloadJavaLzma(javaUrl);

                decompressJavaFile(lzmaPath);

                if (!File.Exists(javaPath))
                    throw new WebException("failed to download");

                if (Rule.OSName != "window")
                    NativeMethods.Chmod(javaPath, NativeMethods.Chmod755);
            }

            return javaPath;
        }

        public Task<string> CheckJavaAsync()
            => CheckJavaAsync(null);

        public async Task<string> CheckJavaAsync(IProgress<ProgressChangedEventArgs>? progress)
        {
            string javapath = GetBinaryPath();

            if (!CheckJavaExistence())
            {
                if (progress == null)
                {
                    pProgressChanged = new Progress<ProgressChangedEventArgs>(
                        (e) => ProgressChanged?.Invoke(this, e));
                }
                else
                {
                    pProgressChanged = progress;
                }

                string javaUrl = await GetJavaUrlAsync().ConfigureAwait(false);
                string lzmaPath = await downloadJavaLzmaAsync(javaUrl).ConfigureAwait(false);

                Task decompressTask = Task.Run(() => decompressJavaFile(lzmaPath));
                await decompressTask.ConfigureAwait(false);

                if (!File.Exists(javapath))
                    throw new WebException("failed to download");

                if (Rule.OSName != "window")
                    NativeMethods.Chmod(javapath, NativeMethods.Chmod755);
            }

            return javapath;
        }

        public string GetJavaUrl()
        {
            string json = LibHttp.GetString(MojangServer.LauncherMeta);
            return parseLauncherMetadata(json);
        }

        public async Task<string> GetJavaUrlAsync()
        {
            string json = await LibHttp.GetStringAsync(MojangServer.LauncherMeta)
                .ConfigureAwait(false);
            return parseLauncherMetadata(json);
        }

        // Major version of a Java install from its "release" file (JAVA_VERSION="17.0.8" -> 17,
        // "1.8.0_381" -> 8). javaBinaryPath is <home>/bin/java(w). Returns 0 when unknown.
        public static int GetMajorVersion(string javaBinaryPath)
        {
            try
            {
                string? binDir = Path.GetDirectoryName(javaBinaryPath);
                string? home = string.IsNullOrEmpty(binDir) ? null : Path.GetDirectoryName(binDir);
                if (string.IsNullOrEmpty(home))
                    return 0;

                string release = Path.Combine(home, "release");
                if (!File.Exists(release))
                    return 0;

                foreach (string line in File.ReadLines(release))
                {
                    if (!line.StartsWith("JAVA_VERSION="))
                        continue;

                    string value = line.Substring("JAVA_VERSION=".Length).Trim().Trim('"');
                    string[] parts = value.Split('.', '_', '-', '+');
                    if (!int.TryParse(parts[0], out int first))
                        return 0;
                    if (first == 1 && parts.Length > 1 && int.TryParse(parts[1], out int second))
                        return second;
                    return first;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return 0;
        }

        private string parseLauncherMetadata(string json)
        {
            var javaUrl = JObject.Parse(json)
                 [Rule.OSName]
                ?[Rule.Arch]
                ?["jre"]
                ?["url"]
                ?.ToString();

            if (string.IsNullOrEmpty(javaUrl))
                throw new PlatformNotSupportedException("Downloading JRE on current OS is not supported. Set JavaPath manually.");
            return javaUrl;
        }

        private string downloadJavaLzma(string javaUrl)
        {
            Directory.CreateDirectory(RuntimeDirectory);
            string lzmapath = Path.Combine(Path.GetTempPath(), "jre.lzma");

            var webdownloader = new WebDownload();
            webdownloader.DownloadProgressChangedEvent += Downloader_DownloadProgressChangedEvent;
            webdownloader.DownloadFile(javaUrl, lzmapath);

            return lzmapath;
        }

        private async Task<string> downloadJavaLzmaAsync(string javaUrl)
        {
            Directory.CreateDirectory(RuntimeDirectory);
            string lzmapath = Path.Combine(Path.GetTempPath(), "jre.lzma");

            var webdownloader = new WebDownload();
            webdownloader.DownloadProgressChangedEvent += Downloader_DownloadProgressChangedEvent;
            await webdownloader.DownloadFileAsync(new DownloadFile(lzmapath, javaUrl))
                .ConfigureAwait(false);

            return lzmapath;
        }

        private void decompressJavaFile(string lzmaPath)
        {
            string zippath = Path.Combine(Path.GetTempPath(), "jre.zip");
            SevenZipWrapper.DecompressFileLZMA(lzmaPath, zippath);

            var z = new SharpZip(zippath);
            z.ProgressEvent += Z_ProgressEvent;
            z.Unzip(RuntimeDirectory);
        }

        private void Z_ProgressEvent(object? sender, int e)
        {
            pProgressChanged?.Report(new ProgressChangedEventArgs(50 + e / 2, null));
        }

        private void Downloader_DownloadProgressChangedEvent(object? sender, ProgressChangedEventArgs e)
        {
            pProgressChanged?.Report(new ProgressChangedEventArgs(e.ProgressPercentage / 2, null));
        }
    }
}
