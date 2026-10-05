using BlockifyLib.Launcher.Downloader;
using BlockifyLib.Launcher.Minecraft;
using BlockifyLib.Launcher.Minecraft.Mojang;
using BlockifyLib.Launcher.Utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;

namespace BlockifyLib.Launcher.src
{
    public interface IJavaPathResolver
    {
        string[] GetInstalledJavaVersions();
        string? GetDefaultJavaBinaryPath();
        string GetJavaBinaryPath(string javaVersionName);
        string GetJavaBinaryPath(string javaVersionName, string osName);
        string GetJavaDirPath(string javaVersionName);
    }

    public class MinecraftJavaPathResolver : IJavaPathResolver
    {
        public static readonly string JreLegacyVersionName = "jre-legacy";
        public static readonly string CmlLegacyVersionName = "m-legacy";

        public MinecraftJavaPathResolver(MinecraftPath path)
        {
            runtimeDirectory = path.Runtime;
        }

        public MinecraftJavaPathResolver(string path)
        {
            runtimeDirectory = path;
        }

        private readonly string runtimeDirectory;

        public string[] GetInstalledJavaVersions()
        {
            var dir = new DirectoryInfo(runtimeDirectory);
            if (!dir.Exists)
                return new string[] { };

            return dir.GetDirectories()
                .Select(x => x.Name)
                .ToArray();
        }

        public string? GetDefaultJavaBinaryPath()
        {
            var javaVersions = GetInstalledJavaVersions();
            string? javaPath = null;

            if (string.IsNullOrEmpty(javaPath) &&
                javaVersions.Contains(MinecraftJavaPathResolver.JreLegacyVersionName))
                javaPath = GetJavaBinaryPath(MinecraftJavaPathResolver.JreLegacyVersionName, Rule.OSName);

            if (string.IsNullOrEmpty(javaPath) &&
                javaVersions.Contains(MinecraftJavaPathResolver.CmlLegacyVersionName))
                javaPath = GetJavaBinaryPath(MinecraftJavaPathResolver.CmlLegacyVersionName, Rule.OSName);

            if (string.IsNullOrEmpty(javaPath) &&
                javaVersions.Length > 0)
                javaPath = GetJavaBinaryPath(javaVersions[0], Rule.OSName);

            return javaPath;
        }

        public string GetJavaBinaryPath(string javaVersionName)
            => GetJavaBinaryPath(javaVersionName, Rule.OSName);

        public string GetJavaBinaryPath(string javaVersionName, string osName)
        {
            return Path.Combine(
                GetJavaDirPath(javaVersionName),
                "bin",
                GetJavaBinaryName(osName));
        }

        public string GetJavaDirPath()
            => GetJavaDirPath(JreLegacyVersionName);

        public string GetJavaDirPath(string javaVersionName)
            => Path.Combine(runtimeDirectory, javaVersionName);

        public string GetJavaBinaryName(string osName)
        {
            string binaryName = "java";
            if (osName == "windows")
                binaryName = "javaw.exe";
            return binaryName;
        }
    }

    // Thrown when the needed Java is not on disk and cannot be downloaded (K-2).
    public class JavaNotAvailableException : Exception
    {
        public JavaNotAvailableException(string component, int majorVersion, Exception? innerException = null)
            : base(buildMessage(majorVersion), innerException)
        {
            Component = component;
            MajorVersion = majorVersion;
        }

        public string Component { get; }
        public int MajorVersion { get; }

        private static string buildMessage(int majorVersion) =>
            (majorVersion > 0 ? $"Для этой версии нужна Java {majorVersion}" : "Для этой версии нужна Java")
            + ", но она ещё не скачана, а скачать её сейчас не получилось. Проверь интернет и попробуй ещё раз.";
    }

    public class JavaChecker : IFileChecker
    {
        public string JavaManifestServer { get; set; } = MojangServer.JavaManifest;
        public bool CheckHash { get; set; } = true;

        // Timeout of each Java manifest request.
        public static TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

        // A runtime verified this recently is used without asking the network.
        public static TimeSpan VerifiedRuntimeTtl { get; set; } = TimeSpan.FromDays(7);

        private const string VerifiedMarkerName = ".blockify-verified";

        public DownloadFile[]? CheckFiles(MinecraftPath path, Version.Version version,
            IProgress<DownloadFileChangedEventArgs>? downloadProgress)
        {
            if (!string.IsNullOrEmpty(version.JavaBinaryPath) && File.Exists(version.JavaBinaryPath))
                return null;

            var javaVersion = version.JavaVersion;
            if (string.IsNullOrEmpty(javaVersion))
                javaVersion = MinecraftJavaPathResolver.JreLegacyVersionName;

            var files = CheckJava(
                javaVersion, version.JavaMajorVersion, path, downloadProgress, out string binPath);

            version.JavaBinaryPath = binPath;
            return files;
        }

        public Task<DownloadFile[]?> CheckFilesTaskAsync(MinecraftPath path, Version.Version version,
            IProgress<DownloadFileChangedEventArgs>? downloadProgress)
        {
            return Task.Run(() => CheckFiles(path, version, downloadProgress));
        }

        public DownloadFile[] CheckJava(string javaVersion, MinecraftPath path,
            IProgress<DownloadFileChangedEventArgs>? downloadProgress, out string binPath)
            => CheckJava(javaVersion, 0, path, downloadProgress, out binPath);

        public DownloadFile[] CheckJava(string javaVersion, int majorVersion, MinecraftPath path,
            IProgress<DownloadFileChangedEventArgs>? downloadProgress, out string binPath)
        {
            var javaPathResolver = new MinecraftJavaPathResolver(path);
            binPath = javaPathResolver.GetJavaBinaryPath(javaVersion, Rule.OSName);
            string javaDir = javaPathResolver.GetJavaDirPath(javaVersion);

            // Runtime is on disk and was fully verified recently: no network needed.
            if (File.Exists(binPath) && isRecentlyVerified(javaDir))
                return new DownloadFile[] { };

            try
            {
                var osName = getJavaOSName(); // safe
                var javaVersions = getJavaVersionsForOs(osName); // Net, JsonParse Exception
                if (javaVersions != null)
                {
                    // Net, JsonParse
                    var javaManifest = getJavaVersionManifest(javaVersions, javaVersion);

                    if (javaManifest == null && javaVersion != MinecraftJavaPathResolver.JreLegacyVersionName)
                        javaManifest = getJavaVersionManifest(javaVersions, MinecraftJavaPathResolver.JreLegacyVersionName);
                    if (javaManifest == null)
                        return legacyJavaChecker(path, out binPath);

                    var files = javaManifest["files"] as JObject;
                    if (files == null)
                        return legacyJavaChecker(path, out binPath);

                    var result = toDownloadFiles(files, javaDir, downloadProgress);
                    if (result.Length == 0)
                        markVerified(javaDir);
                    return result;
                }
                else
                    return legacyJavaChecker(path, out binPath);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);

                // Offline: a runtime that is already on disk is good enough.
                if (File.Exists(binPath))
                    return new DownloadFile[] { };

                throw new JavaNotAvailableException(javaVersion, majorVersion, e);
            }
        }

        private static bool isRecentlyVerified(string javaDir)
        {
            try
            {
                var marker = new FileInfo(Path.Combine(javaDir, VerifiedMarkerName));
                return marker.Exists && DateTime.UtcNow - marker.LastWriteTimeUtc < VerifiedRuntimeTtl;
            }
            catch
            {
                return false;
            }
        }

        private static void markVerified(string javaDir)
        {
            try
            {
                if (Directory.Exists(javaDir))
                    File.WriteAllText(Path.Combine(javaDir, VerifiedMarkerName), DateTime.UtcNow.ToString("o"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private string getJavaOSName()
        {
            string osName = "";

            if (Rule.OSName == "windows")
                osName = Rule.Arch == "64" ? "windows-x64" : "windows-x86";
            else if (Rule.OSName == "linux")
                osName = Rule.Arch == "64" ? "linux" : "linux-i386";
            else if (Rule.OSName == "osx")
                osName = "mac-os";

            return osName;
        }

        private JObject? getJavaVersionsForOs(string osName)
        {
            string response = LibHttp.GetString(JavaManifestServer, RequestTimeout);
            return JObject.Parse(response)[osName] as JObject;
        }

        private JObject? getJavaVersionManifest(JObject job, string version)
        {
            var versionArr = job[version] as JArray;
            if (versionArr == null || versionArr.Count == 0)
                return null;

            var manifestUrl = versionArr[0]["manifest"]?["url"]?.ToString();
            if (string.IsNullOrEmpty(manifestUrl))
                return null;

            string response = LibHttp.GetString(manifestUrl, RequestTimeout); // ex
            return JObject.Parse(response); // ex
        }

        private DownloadFile[] toDownloadFiles(JObject manifest, string path,
            IProgress<DownloadFileChangedEventArgs>? progress)
        {
            var progressed = 0;
            var files = new List<DownloadFile>(manifest.Count);
            foreach (var prop in manifest)
            {
                var name = prop.Key;
                var value = prop.Value;

                var type = value?["type"]?.ToString();
                if (type == "file")
                {
                    var filePath = Path.Combine(path, name);
                    filePath = IOUtil.NormalizePath(filePath);

                    bool.TryParse(value?["executable"]?.ToString(), out bool executable);

                    var file = checkJavaFile(value, filePath);
                    if (file != null)
                    {
                        file.Name = name;
                        if (executable)
                            file.AfterDownload = new Func<Task>[]
                            {
                                () => Task.Run(() => tryChmod755(filePath))
                            };
                        files.Add(file);
                    }
                }
                else
                {
                    if (type != "directory")
                        Debug.WriteLine(type);
                }

                progressed++;
                progress?.Report(new DownloadFileChangedEventArgs(
                    TypeFile.Runtime, this, name, manifest.Count, progressed));
            }
            return files.ToArray();
        }

        private DownloadFile? checkJavaFile(JToken? value, string filePath)
        {
            var downloadObj = value?["downloads"]?["raw"];
            if (downloadObj == null)
                return null;

            var url = downloadObj["url"]?.ToString();
            if (string.IsNullOrEmpty(url))
                return null;

            var hash = downloadObj["sha1"]?.ToString();
            var sizeStr = downloadObj["size"]?.ToString();
            long.TryParse(sizeStr, out long size);

            if (IOUtil.CheckFileValidation(filePath, hash, CheckHash))
                return null;

            return new DownloadFile(filePath, url)
            {
                Type = TypeFile.Runtime,
                Size = size,
                Hash = hash
            };
        }

        private DownloadFile[] legacyJavaChecker(MinecraftPath path, out string binPath)
        {
            var javaPathResolver = new MinecraftJavaPathResolver(path);
            string legacyJavaPath = javaPathResolver.GetJavaDirPath(MinecraftJavaPathResolver.CmlLegacyVersionName);

            Java mJava = new Java(legacyJavaPath);
            binPath = mJava.GetBinaryPath();

            try
            {
                if (mJava.CheckJavaExistence())
                    return new DownloadFile[] { };

                string javaUrl = mJava.GetJavaUrl();
                string lzmaPath = Path.Combine(Path.GetTempPath(), "jre.lzma");
                string zipPath = Path.Combine(Path.GetTempPath(), "jre.zip");

                DownloadFile file = new DownloadFile(lzmaPath, javaUrl)
                {
                    Name = "jre.lzma",
                    Type = TypeFile.Runtime,
                    AfterDownload = new Func<Task>[]
                    {
                        () => Task.Run(() =>
                        {
                            SevenZipWrapper.DecompressFileLZMA(lzmaPath, zipPath);

                            var z = new SharpZip(zipPath);
                            z.Unzip(legacyJavaPath);

                            tryChmod755(mJava.GetBinaryPath());
                        })
                    }
                };

                return new[] { file };
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return new DownloadFile[] { };
            }
        }

        private void tryChmod755(string path)
        {
            try
            {
                if (Rule.OSName != "window")
                    NativeMethods.Chmod(path, NativeMethods.Chmod755);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}
