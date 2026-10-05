using BlockifyLib.Launcher.Downloader;
using BlockifyLib.Launcher.Minecraft;
using BlockifyLib.Launcher.src;
using BlockifyLib.Launcher.Version;
using BlockifyLib.Launcher.Version.Load;
using BlockifyLib.Launcher.Version.Metadata;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace BlockifyLib
{
    public class BlockifyLibLauncher
    {
        public BlockifyLibLauncher(string path) : this(new MinecraftPath(path)) { }

        public BlockifyLibLauncher(MinecraftPath path)
        {
            this.MinecraftPath = path;

            GameFileCheckers = new FileCheckerCollection();
            FileDownloader = new AsyncDownloadFile();
            VersionLoader = new DefaultVersionLoader(MinecraftPath);

            pFileChanged = new Progress<DownloadFileChangedEventArgs>(
                e => FileChanged?.Invoke(e));
            pProgressChanged = new Progress<ProgressChangedEventArgs>(
                e => ProgressChanged?.Invoke(this, e));

            JavaPathResolver = new MinecraftJavaPathResolver(path);
        }

        public event DownloadFileChangedHandler? FileChanged;
        public event ProgressChangedEventHandler? ProgressChanged;
        public event EventHandler<string>? LogOutput;

        private readonly IProgress<DownloadFileChangedEventArgs> pFileChanged;
        private readonly IProgress<ProgressChangedEventArgs> pProgressChanged;

        public MinecraftPath MinecraftPath { get; private set; }
        public VersionCollection? Versions { get; private set; }
        public IVersionLoader VersionLoader { get; set; }

        public FileCheckerCollection GameFileCheckers { get; private set; }
        public IDownloader? FileDownloader { get; set; }

        public IJavaPathResolver JavaPathResolver { get; set; }

        public VersionCollection GetAllVersions()
        {
            Versions = VersionLoader.GetVersionMetadatas();
            return Versions;
        }

        public async Task<VersionCollection> GetAllVersionsAsync()
        {
            Versions = await VersionLoader.GetVersionMetadatasAsync()
                .ConfigureAwait(false);
            return Versions;
        }

        public Launcher.Version.Version GetVersion(string versionName)
        {
            if (Versions == null)
                GetAllVersions();

            preferLocalVersion(versionName);
            return Versions!.GetVersion(versionName);
        }

        public async Task<Launcher.Version.Version> GetVersionAsync(string versionName)
        {
            if (Versions == null)
                await GetAllVersionsAsync().ConfigureAwait(false);

            preferLocalVersion(versionName);
            var version = await Versions!.GetVersionAsync(versionName)
                .ConfigureAwait(false);
            return version;
        }

        // A version installed after the list was loaded (or listed only as a web version)
        // is read from versions/<id>/<id>.json, so it starts without network.
        private void preferLocalVersion(string versionName)
        {
            if (Versions == null || string.IsNullOrEmpty(versionName))
                return;

            try
            {
                if (Versions.Contains(versionName) && Versions.GetVersionMetadata(versionName).IsLocalVersion)
                    return;

                string jsonPath = MinecraftPath.GetVersionJsonPath(versionName);
                if (!File.Exists(jsonPath))
                    return;

                Versions.AddVersion(new LocalVersion(versionName)
                {
                    Path = jsonPath,
                    Type = "local",
                    ProfType = ProfileConverter.VersionType.Custom
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public async Task<DownloadFile[]> CheckLostGameFilesTaskAsync(Launcher.Version.Version version)
        {
            var lostFiles = new List<DownloadFile>();
            foreach (IFileChecker checker in this.GameFileCheckers)
            {
                DownloadFile[]? files = await checker.CheckFilesTaskAsync(MinecraftPath, version, pFileChanged)
                    .ConfigureAwait(false);
                if (files != null)
                    lostFiles.AddRange(files);
            }

            return lostFiles.ToArray();
        }

        public async Task DownloadGameFiles(DownloadFile[] files)
        {
            if (this.FileDownloader == null)
                return;

            await FileDownloader.DownloadFiles(files, pFileChanged, pProgressChanged)
                .ConfigureAwait(false);
        }

        public void CheckAndDownload(Launcher.Version.Version version)
        {
            foreach (var checker in this.GameFileCheckers)
            {
                DownloadFile[]? files = checker.CheckFiles(MinecraftPath, version, pFileChanged);

                if (files == null || files.Length == 0)
                    continue;

                DownloadGameFiles(files).GetAwaiter().GetResult();
            }
        }

        public async Task CheckAndDownloadAsync(Launcher.Version.Version version)
        {
            foreach (var checker in this.GameFileCheckers)
            {
                DownloadFile[]? files = await checker.CheckFilesTaskAsync(MinecraftPath, version, pFileChanged)
                    .ConfigureAwait(false);

                if (files == null || files.Length == 0)
                    continue;

                await DownloadGameFiles(files).ConfigureAwait(false);
            }
        }

        public Process CreateProcess(string versionName, LaunchOption option, bool checkAndDownload = true)
            => CreateProcess(GetVersion(versionName), option, checkAndDownload);

        public Process CreateProcess(Launcher.Version.Version version, LaunchOption option, bool checkAndDownload = true)
        {
            option.StartVersion = version;
            applyCustomJava(option);

            if (checkAndDownload)
                CheckAndDownload(option.StartVersion);

            return CreateProcess(option);
        }

        public async Task<Process> CreateProcessAsync(string versionName, LaunchOption option,
            bool checkAndDownload = true)
        {
            var version = await GetVersionAsync(versionName).ConfigureAwait(false);
            return await CreateProcessAsync(version, option, checkAndDownload).ConfigureAwait(false);
        }

        public async Task<Process> CreateProcessAsync(Launcher.Version.Version version, LaunchOption option,
            bool checkAndDownload = true)
        {
            option.StartVersion = version;
            applyCustomJava(option);

            if (checkAndDownload)
                await CheckAndDownloadAsync(option.StartVersion).ConfigureAwait(false);

            return await CreateProcessAsync(option).ConfigureAwait(false);
        }

        public Process CreateProcess(LaunchOption option)
        {
            checkLaunchOption(option);
            var launch = new Launch(option);
            return launch.GetProcess();
        }

        public async Task<Process> CreateProcessAsync(LaunchOption option)
        {
            checkLaunchOption(option);
            var launch = new Launch(option);
            return await Task.Run(launch.GetProcess).ConfigureAwait(false);
        }

        public Process Launch(string versionName, LaunchOption option)
        {
            Process process = CreateProcess(versionName, option);
            process.Start();
            return process;
        }

        public async Task<Process> LaunchAsync(string versionName, LaunchOption option)
        {
            Process process = await CreateProcessAsync(versionName, option)
                .ConfigureAwait(false);
            process.Start();
            return process;
        }

        // Custom Java goes in before the file check, so JavaChecker doesn't need the bundled
        // runtime (or the network) for it. An obviously too old Java is ignored.
        private void applyCustomJava(LaunchOption option)
        {
            var version = option.StartVersion;
            if (version != null && !string.IsNullOrEmpty(option.JavaPath)
                && isCustomJavaUsable(option.JavaPath, version, false))
                version.JavaBinaryPath = option.JavaPath;
        }

        // false when the version json asks for a newer major Java than the custom one.
        private bool isCustomJavaUsable(string javaPath, Launcher.Version.Version version, bool log)
        {
            int required = version.JavaMajorVersion;
            if (required <= 0)
                return true;

            int actual = Java.GetMajorVersion(javaPath);
            if (actual <= 0 || actual >= required)
                return true;

            if (log)
                LogOutput?.Invoke(this,
                    $"Custom Java {actual} ({javaPath}) is too old for {version.id} (needs Java {required}), using the bundled runtime");
            return false;
        }

        private void checkLaunchOption(LaunchOption option)
        {
            if (option.Path == null)
                option.Path = MinecraftPath;
            if (option.StartVersion != null)
            {
                if (!string.IsNullOrEmpty(option.JavaPath)
                    && isCustomJavaUsable(option.JavaPath, option.StartVersion, true))
                    option.StartVersion.JavaBinaryPath = option.JavaPath;
                else if (!string.IsNullOrEmpty(option.JavaVersion))
                    option.StartVersion.JavaVersion = option.JavaVersion;
                else if (string.IsNullOrEmpty(option.StartVersion.JavaBinaryPath))
                    option.StartVersion.JavaBinaryPath =
                        GetJavaPath(option.StartVersion) ?? GetDefaultJavaPath();
            }
        }

        public string? GetJavaPath(Launcher.Version.Version version)
        {
            if (string.IsNullOrEmpty(version.JavaVersion))
                return null;

            return JavaPathResolver.GetJavaBinaryPath(version.JavaVersion, Launcher.Rule.OSName);
        }

        public string? GetDefaultJavaPath() => JavaPathResolver.GetDefaultJavaBinaryPath();
    }
}
