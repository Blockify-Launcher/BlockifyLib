using BlockifyLib.Launcher.Downloader;
using BlockifyLib.Launcher.Minecraft;
using BlockifyLib.Launcher.src;
using BlockifyLib.Launcher.Version;
using BlockifyLib.Launcher.Version.Load;
using System.ComponentModel;
using System.Diagnostics;

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

            return Versions!.GetVersion(versionName);
        }

        public async Task<Launcher.Version.Version> GetVersionAsync(string versionName)
        {
            if (Versions == null)
                await GetAllVersionsAsync().ConfigureAwait(false);

            var version = await Versions!.GetVersionAsync(versionName)
                .ConfigureAwait(false);
            return version;
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

        private void checkLaunchOption(LaunchOption option)
        {
            if (option.Path == null)
                option.Path = MinecraftPath;
            if (option.StartVersion != null)
            {
                if (!string.IsNullOrEmpty(option.JavaPath))
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
