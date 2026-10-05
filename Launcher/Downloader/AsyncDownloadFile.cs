using BlockifyLib.Launcher.Utils;
using System.Collections.Concurrent;
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

        // Values of the most recent DownloadFiles call (kept for compatibility).
        public int totalFiles { get; set; }
        public long totalBytes { get; set; }
        public long receivedBytes { get; set; }
        public int progressedFiles;

        // Extra attempts per file after the first failure (pauses 1, 2, 4 s ...).
        public int RetryCount { get; set; } = 3;

        // true = skip files that still failed after retries instead of throwing DownloadFileException.
        public bool IgnoreInvalidFiles = false;

        public AsyncDownloadFile() : this(10) { }
        public AsyncDownloadFile(int paral) => MaxThread = paral;

        // State of one DownloadFiles call, so parallel calls don't clash.
        private sealed class Session
        {
            public readonly object Lock = new object();
            public int TotalFiles;
            public int ProgressedFiles;
            public long TotalBytes;
            public long ReceivedBytes;
            public readonly ConcurrentQueue<DownloadFile> Failed = new ConcurrentQueue<DownloadFile>();
            public Exception? LastError;
            public IProgress<DownloadFileChangedEventArgs>? FileProgress;
            public IProgress<ProgressChangedEventArgs>? ByteProgress;
        }

        public async Task DownloadFiles(DownloadFile[] files, IProgress<DownloadFileChangedEventArgs>? fileProgress,
            IProgress<ProgressChangedEventArgs>? downloadProgress)
        {
            if (files.Length == 0)
                return;

            var session = new Session
            {
                TotalFiles = files.Length,
                FileProgress = fileProgress,
                ByteProgress = downloadProgress
            };

            foreach (DownloadFile item in files)
                if (item.Size > 0)
                    session.TotalBytes += item.Size;

            totalFiles = session.TotalFiles;
            totalBytes = session.TotalBytes;
            receivedBytes = 0;
            progressedFiles = 0;

            fileProgress?.Report(
                new DownloadFileChangedEventArgs(files[0].Type, this, null, files.Length, 0));
            await ForEachAsyncSemaphore(files, MaxThread, file => doDownload(session, file))
                .ConfigureAwait(false);

            if (!session.Failed.IsEmpty && !IgnoreInvalidFiles)
            {
                var failed = session.Failed.ToArray();
                var names = failed
                    .Select(f => f.Name ?? System.IO.Path.GetFileName(f.Path))
                    .ToList();
                throw new DownloadFileException(names, session.LastError, failed[0]);
            }
        }

        private async Task ForEachAsyncSemaphore<T>(IEnumerable<T> source,
                            int degreeOfParallelism, Func<T, Task> body)
        {
            List<Task> tasks = new List<Task>();
            using SemaphoreSlim throttler = new SemaphoreSlim(Math.Max(1, degreeOfParallelism));
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

        // Never throws: a file that fails every attempt goes to session.Failed.
        private async Task doDownload(Session session, DownloadFile file)
        {
            string original = file.Url;
            string rewritten = NetHooks.Rewrite(original);
            int attempts = Math.Max(0, RetryCount) + 1;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                if (attempt > 0)
                    await Task.Delay(TimeSpan.FromSeconds(1 << Math.Min(attempt - 1, 4))).ConfigureAwait(false);

                // The last attempt goes to the original host in case the mirror is broken.
                string url = attempt == attempts - 1 && attempts > 1 ? original : rewritten;

                long attemptBytes = 0;
                var downloader = new WebDownload();
                downloader.FileDownloadProgressChanged += (sender, e) =>
                {
                    attemptBytes += e.ProgressedBytes;
                    onBytes(session, e);
                };

                try
                {
                    await downloader.DownloadFileAsync(file, url, CancellationToken.None).ConfigureAwait(false);

                    if (file.AfterDownload != null)
                        foreach (var item in file.AfterDownload)
                            await item.Invoke().ConfigureAwait(false);

                    int done = Interlocked.Increment(ref session.ProgressedFiles);
                    progressedFiles = done;
                    session.FileProgress?.Report(
                        new DownloadFileChangedEventArgs(file.Type, this, file.Name, session.TotalFiles, done));
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"download failed ({attempt + 1}/{attempts}) {url}: {ex.Message}");
                    session.LastError = ex;

                    // Bytes of a failed attempt don't count towards the overall progress.
                    lock (session.Lock)
                    {
                        session.ReceivedBytes = Math.Max(0, session.ReceivedBytes - attemptBytes);
                        receivedBytes = session.ReceivedBytes;
                    }
                }
            }

            session.Failed.Enqueue(file);
        }

        private void onBytes(Session session, DownloadFileProgress e)
        {
            FileProgressChangedEventArgs? args = null;
            lock (session.Lock)
            {
                if (e.File.Size <= 0 && e.TotalBytes > 0)
                {
                    session.TotalBytes += e.TotalBytes;
                    e.File.Size = e.TotalBytes;
                }

                session.ReceivedBytes += e.ProgressedBytes;
                totalBytes = session.TotalBytes;
                receivedBytes = session.ReceivedBytes;

                if (session.TotalBytes > 0 && session.ReceivedBytes <= session.TotalBytes)
                {
                    int percent = (int)((float)session.ReceivedBytes / session.TotalBytes * 100);
                    args = new FileProgressChangedEventArgs(session.TotalBytes, session.ReceivedBytes, percent);
                }
            }

            if (args != null)
                session.ByteProgress?.Report(args);
        }
    }
}
