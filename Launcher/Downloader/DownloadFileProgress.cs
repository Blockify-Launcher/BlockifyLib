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

    public class DownloadFileException : Exception
    {
        public DownloadFileException(DownloadFile exFile)
            : this(null, null, exFile) { }

        public DownloadFileException(string? message, Exception? innerException, DownloadFile? exFile)
            : base(message, innerException)
        {
            ExceptionFile = exFile;
        }

        public DownloadFile? ExceptionFile { get; private set; }
    }
}
