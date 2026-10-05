namespace BlockifyLib.Launcher.Downloader
{
    public class DownloadFileProgress
    {
        public DownloadFileProgress(DownloadFile file, long total, long progressed, long received, int percent)
        {
            this.File = file;
            this.TotalBytes = total;
            this.ProgressedBytes = progressed;
            this.ReceivedBytes = received;
            this.ProgressPercentage = percent;
        }

        public DownloadFile File { get; private set; }
        public long TotalBytes { get; private set; }
        public long ProgressedBytes { get; private set; }
        public long ReceivedBytes { get; private set; }
        public int ProgressPercentage { get; private set; }
    }

    public delegate void DownloadFileChangedHandler(DownloadFileChangedEventArgs e);

    public enum TypeFile { Runtime, Library, Resource, Minecraft, Others }

    public class DownloadFileChangedEventArgs : EventArgs
    {
        public DownloadFileChangedEventArgs(TypeFile type, object source, string? filename, int total, int progressed)
        {
            FileKind = type;
            FileType = type;
            FileName = filename;
            TotalFileCount = total;
            ProgressedFileCount = progressed;
            Source = source;
        }

        public TypeFile FileType { get; private set; }
        public TypeFile FileKind { get; private set; }
        public string? FileName { get; private set; }
        public int TotalFileCount { get; private set; }
        public int ProgressedFileCount { get; private set; }
        public object Source { get; private set; }
    }

    // Thrown when game files are still missing after all retries (K-2).
    public class DownloadFileException : Exception
    {
        public DownloadFileException(DownloadFile exFile)
            : this((string?)null, null, exFile) { }

        public DownloadFileException(string? message, Exception? innerException, DownloadFile? exFile)
            : base(message, innerException)
        {
            ExceptionFile = exFile;
            FailedFiles = exFile == null
                ? Array.Empty<string>()
                : new[] { exFile.Name ?? exFile.Path };
        }

        public DownloadFileException(IReadOnlyList<string> failedFiles, Exception? innerException,
            DownloadFile? firstFile = null)
            : base(BuildMessage(failedFiles.Count), innerException)
        {
            FailedFiles = failedFiles;
            ExceptionFile = firstFile;
        }

        public DownloadFile? ExceptionFile { get; private set; }

        // Names (or paths) of the files that could not be downloaded.
        public IReadOnlyList<string> FailedFiles { get; }

        public static string BuildMessage(int count)
        {
            int mod10 = count % 10, mod100 = count % 100;
            string word = mod10 == 1 && mod100 != 11 ? "файл"
                : mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14) ? "файла"
                : "файлов";
            return $"Не удалось скачать {count} {word} игры. Проверь интернет и попробуй ещё раз.";
        }
    }
}
