using System.ComponentModel;

namespace BlockifyLib.Launcher.Downloader
{
    public interface IDownloader
    {
        Task DownloadFiles(DownloadFile[] files,
            IProgress<DownloadFileChangedEventArgs>? fileProgress,
            IProgress<ProgressChangedEventArgs>? downloadProgress);
    }
}
