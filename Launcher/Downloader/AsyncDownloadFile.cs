using BlockifyLib.Launcher.Utils;
using System.ComponentModel;
using System.Diagnostics;

namespace BlockifyLib.Launcher.Downloader
{
    public class FileProgressChangedEventArgs : ProgressChangedEventArgs
    {
        public FileProgressChangedEventArgs(long total, long received, int percent) : base(percent, null)
        {
            this.TotalBytes = total;
            this.ReceivedBytes = received;
        }

        public long TotalBytes { get; private set; }
        public long ReceivedBytes { get; private set; }
    }

    public class AsyncDownloadFile : IDownloader
    {
        public int MaxThread { get; private set; }
        public int totalFiles { get; set; }
        public long totalBytes { get; set; }
        public long receivedBytes { get; set; }

        public int progressedFiles;
        public bool IgnoreInvalidFiles = true;
        private readonly object progressEventLock = new object();
        private bool isWork;

        private IProgress<FileProgressChangedEventArgs>? processChangeProgress;
        private IProgress<DownloadFileChangedEventArgs>? processChangeFile;

        public AsyncDownloadFile() : this(10) { }
        public AsyncDownloadFile(int paral) => MaxThread = paral;

        public async Task DownloadFiles(DownloadFile[] files, IProgress<DownloadFileChangedEventArgs>? fileProgress,
            IProgress<ProgressChangedEventArgs>? downloadProgress)
        {
            if (files.Length == 0)
                return;
            if (isWork)
                throw new InvalidOperationException("already downloading");

            isWork = true;
            processChangeFile = fileProgress;
            processChangeProgress = downloadProgress;

            totalFiles = files.Length;
            progressedFiles = 0;

            totalBytes = 0;
            receivedBytes = 0;

            foreach (DownloadFile item in files)
                if (item.Size > 0)
                    totalBytes += item.Size;

            fileProgress?.Report(
                new DownloadFileChangedEventArgs(files[0].Type, this, null, files.Length, 0));
            await ForEachAsyncSemaphore(files, MaxThread, doDownload).ConfigureAwait(false);
            isWork = false;
        }

        private async Task ForEachAsyncSemaphore<T>(IEnumerable<T> source,
                            int degreeOfParallelism, Func<T, Task> body)
        {
            List<Task> tasks = new List<Task>();
            using SemaphoreSlim throttler = new SemaphoreSlim(degreeOfParallelism);
            foreach (var element in source)
            {
                await throttler.WaitAsync().ConfigureAwait(false);
                async Task work(T item)
                {
                    try
                    {
                        await body(item).ConfigureAwait(false);
                    }
                    finally
                    {
                        throttler.Release();
                    }
                }

                tasks.Add(work(element));
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task doDownload(DownloadFile file)
        {
            try
            {
                await doDownload(file, 3).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!IgnoreInvalidFiles)
                    throw new DownloadFileException("failed to download", ex, file);
            }
        }

        private async Task doDownload(DownloadFile file, int retry)
        {
            try
            {
                WebDownload downloader = new WebDownload();
                downloader.FileDownloadProgressChanged += Downloader_FileDownloadProgressChanged;

                await downloader.DownloadFileAsync(file).ConfigureAwait(false);

                if (file.AfterDownload != null)
                    foreach (var item in file.AfterDownload)
                        await item.Invoke().ConfigureAwait(false);

                Interlocked.Increment(ref progressedFiles);
                processChangeFile?.Report(
                    new DownloadFileChangedEventArgs(file.Type, this, file.Name, totalFiles, progressedFiles));
            }
            catch (Exception ex)
            {
                if (retry <= 0)
                    return;

                Debug.WriteLine(ex);
                retry--;

                await doDownload(file, retry).ConfigureAwait(false);
            }
        }

        private void Downloader_FileDownloadProgressChanged(object? sender, DownloadFileProgress e)
        {
            lock (progressEventLock)
            {
                if (e.File.Size <= 0)
                {
                    totalBytes += e.TotalBytes;
                    e.File.Size = e.TotalBytes;
                }

                receivedBytes += e.ProgressedBytes;

                if (receivedBytes > totalBytes)
                    return;

                float percent = (float)receivedBytes / totalBytes * 100;
                if (percent >= 0)
                    processChangeProgress?.Report(new FileProgressChangedEventArgs(totalBytes, receivedBytes, (int)percent));
                else
                    Debug.WriteLine($"total: {totalBytes} received: {receivedBytes} filename: {e.File.Name} filesize: {e.File.Size}");
            }
        }
    }
}
