using BlockifyLib.Launcher.Downloader;
using BlockifyLib.Launcher.Minecraft;
using BlockifyLib.Launcher.Minecraft.Mojang;
using BlockifyLib.Launcher.Utils;
using BlockifyLib.Launcher.Version.Func;
using System.Collections;
using System.IO;

namespace BlockifyLib.Launcher.src
{
    public interface IFileChecker
    {
        DownloadFile[]? CheckFiles(MinecraftPath path, Version.Version version,
            IProgress<DownloadFileChangedEventArgs>? downloadProgress);
        Task<DownloadFile[]?> CheckFilesTaskAsync(MinecraftPath path, Version.Version version,
            IProgress<DownloadFileChangedEventArgs>? downloadProgress);
    }

    public class FileCheckerCollection : IEnumerable<IFileChecker>
    {
        public IFileChecker this[int index] => checkers[index];

        private readonly List<IFileChecker> checkers;

        private AssetChecker? asset;
        public AssetChecker? AssetFileChecker
        {
            get => asset;
            set
            {
                if (asset != null)
                    checkers.Remove(asset);

                asset = value;

                if (asset != null)
                    checkers.Add(asset);
            }
        }

        private ClientChecker? client;
        public ClientChecker? ClientFileChecker
        {
            get => client;
            set
            {
                if (client != null)
                    checkers.Remove(client);

                client = value;

                if (client != null)
                    checkers.Add(client);
            }
        }

        private LibraryChecker? library;
        public LibraryChecker? LibraryFileChecker
        {
            get => library;
            set
            {
                if (library != null)
                    checkers.Remove(library);

                library = value;

                if (library != null)
                    checkers.Add(library);
            }
        }

        private JavaChecker? java;

        public JavaChecker? JavaFileChecker
        {
            get => java;
            set
            {
                if (java != null)
                    checkers.Remove(java);

                java = value;

                if (java != null)
                    checkers.Add(java);
            }
        }

        private LogChecker? log;
        public LogChecker? LogFileChecker
        {
            get => log;
            set
            {
                if (log != null)
                    checkers.Remove(log);

                log = value;

                if (log != null)
                    checkers.Add(log);
            }
        }

        public FileCheckerCollection()
        {
            checkers = new List<IFileChecker>(4);

            library = new LibraryChecker();
            asset = new AssetChecker();
            client = new ClientChecker();
            java = new JavaChecker();
            log = new LogChecker();

            checkers.Add(library);
            checkers.Add(asset);
            checkers.Add(client);
            checkers.Add(log);
            checkers.Add(java);
        }

        public void Add(IFileChecker item)
        {
            CheckArgument(item);
            checkers.Add(item);
        }

        public void AddRange(IEnumerable<IFileChecker?> items)
        {
            foreach (IFileChecker? item in items)
            {
                if (item != null)
                    Add(item);
            }
        }

        public void Remove(IFileChecker item)
        {
            CheckArgument(item);
            checkers.Remove(item);
        }

        public void RemoveAt(int index)
        {
            IFileChecker item = checkers[index];
            Remove(item);
        }

        public void Insert(int index, IFileChecker item)
        {
            CheckArgument(item);
            checkers.Insert(index, item);
        }

        private void CheckArgument(IFileChecker item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item is LibraryChecker)
                throw new ArgumentException($"Set {nameof(LibraryFileChecker)} property.");
            if (item is AssetChecker)
                throw new ArgumentException($"Set {nameof(AssetFileChecker)} property.");
            if (item is ClientChecker)
                throw new ArgumentException($"Set {nameof(ClientFileChecker)} property.");
            if (item is JavaChecker)
                throw new ArgumentException($"Set {nameof(JavaFileChecker)} property.");
        }

        public IEnumerator<IFileChecker> GetEnumerator()
        {
            return checkers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return checkers.GetEnumerator();
        }
    }

    public sealed class ClientChecker : IFileChecker
    {
        public bool CheckHash { get; set; } = true;

        public DownloadFile[]? CheckFiles(MinecraftPath path, Version.Version version,
            IProgress<DownloadFileChangedEventArgs>? progress)
        {
            progress?.Report(new DownloadFileChangedEventArgs(TypeFile.Minecraft, this, version.Jar, 1, 0));
            DownloadFile? result = checkClientFile(path, version);
            progress?.Report(new DownloadFileChangedEventArgs(TypeFile.Minecraft, this, version.Jar, 1, 1));

            if (result == null)
                return null;
            else
                return new[] { result };
        }

        public async Task<DownloadFile[]?> CheckFilesTaskAsync(MinecraftPath path, Version.Version version,
            IProgress<DownloadFileChangedEventArgs>? progress)
        {
            progress?.Report(new DownloadFileChangedEventArgs(TypeFile.Minecraft, this, version.Jar, 1, 0));
            DownloadFile? result = await Task.Run(() => checkClientFile(path, version))
                .ConfigureAwait(false);
            progress?.Report(new DownloadFileChangedEventArgs(TypeFile.Minecraft, this, version.Jar, 1, 1));

            if (result == null)
                return null;
            else
                return new[] { result };
        }

        private DownloadFile? checkClientFile(MinecraftPath path, Version.Version version)
        {
            if (string.IsNullOrEmpty(version.ClientDownloadUrl)
                || string.IsNullOrEmpty(version.Jar))
                return null;

            string id = version.Jar;
            string clientPath = path.GetVersionJarPath(id);

            if (!IOUtil.CheckFileValidation(clientPath, version.ClientHash, CheckHash))
            {
                return new DownloadFile(clientPath, version.ClientDownloadUrl)
                {
                    Type = TypeFile.Minecraft,
                    Name = id
                };
            }
            else
                return null;
        }
    }

    public sealed class LibraryChecker : IFileChecker
    {
        private string libServer = MojangServer.Library;
        public string LibraryServer
        {
            get => libServer;
            set
            {
                if (value.Last() == '/')
                    libServer = value;
                else
                    libServer = value + "/";
            }
        }
        public bool CheckHash { get; set; } = true;

        public DownloadFile[]? CheckFiles(MinecraftPath path, Version.Version version,
            IProgress<DownloadFileChangedEventArgs>? progress)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));
            return CheckFiles(path, version.Libraries, progress);
        }

        public Task<DownloadFile[]?> CheckFilesTaskAsync(MinecraftPath path, Version.Version version,
            IProgress<DownloadFileChangedEventArgs>? progress)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            return Task.Run(() => checkLibraries(path, version.Libraries, progress));
        }

        public DownloadFile[]? CheckFiles(MinecraftPath path, Library[]? libs,
            IProgress<DownloadFileChangedEventArgs>? progress)
        {
            return checkLibraries(path, libs, progress);
        }

        private DownloadFile[]? checkLibraries(MinecraftPath path, Library[]? libs,
            IProgress<DownloadFileChangedEventArgs>? progress)
        {
            if (libs == null)
                throw new ArgumentNullException(nameof(libs));

            if (libs.Length == 0)
                return null;

            int progressed = 0;
            var files = new List<DownloadFile>(libs.Length);
            foreach (Library library in libs)
            {
                bool downloadRequire = checkDownloadRequire(path, library);

                if (downloadRequire)
                {
                    string libPath = Path.Combine(path.Library, library.Path!);
                    string? libUrl = createDownloadUrl(library);

                    if (!string.IsNullOrEmpty(libUrl))
                    {
                        files.Add(new DownloadFile(libPath, libUrl)
                        {
                            Type = TypeFile.Library,
                            Name = library.Name,
                            Size = library.Size
                        });
                    }
                }

                progressed++;
                progress?.Report(new DownloadFileChangedEventArgs(
                    TypeFile.Library, this, library.Name, libs.Length, progressed));
            }
            return files.Distinct().ToArray();
        }

        private string? createDownloadUrl(Library lib)
        {
            string? url = lib.Url;

            if (url == null)
                url = LibraryServer + lib.Path;
            else if (url == "")
                url = null;
            else if (url.Split('/').Last() == "")
                url += lib.Path?.Replace("\\", "/");

            return url;
        }

        private bool checkDownloadRequire(MinecraftPath path, Library lib)
        {
            return lib.IsRequire
                   && !string.IsNullOrEmpty(lib.Path)
                   && !IOUtil.CheckFileValidation(Path.Combine(path.Library, lib.Path), lib.Hash, CheckHash);
        }
    }
}
