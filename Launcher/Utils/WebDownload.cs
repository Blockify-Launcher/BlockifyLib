using BlockifyLib.Launcher.Downloader;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace BlockifyLib.Launcher.Utils
{
    // Streams one file into "<file>.part", resumes it with Range, checks size and sha1,
    // and only then moves it into place, so a broken download never looks like a valid file.
    internal class WebDownload
    {
        // Max wait for response headers.
        public static TimeSpan HeadersTimeout { get; set; } = TimeSpan.FromSeconds(30);

        // Max time without a single received byte.
        public static TimeSpan IdleTimeout { get; set; } = TimeSpan.FromSeconds(30);

        private const int DefaultBufferSize = 1024 * 64;
        private const string PartExtension = ".part";

        internal event EventHandler<DownloadFileProgress>? FileDownloadProgressChanged;
        internal event ProgressChangedEventHandler? DownloadProgressChangedEvent;

        // Sync download without hash (legacy Java installer).
        internal void DownloadFile(string url, string path)
        {
            var file = new DownloadFile(path, url);
            Task.Run(() => DownloadFileAsync(file, NetHooks.Rewrite(url), CancellationToken.None))
                .GetAwaiter().GetResult();
        }

        internal Task DownloadFileAsync(DownloadFile file) =>
            DownloadFileAsync(file, NetHooks.Rewrite(file.Url), CancellationToken.None);

        // Sync download with a few retries; used for single metadata files (asset index).
        internal static void DownloadWithRetry(DownloadFile file, int attempts)
        {
            Exception? last = null;
            for (int attempt = 0; attempt < Math.Max(1, attempts); attempt++)
            {
                if (attempt > 0)
                    Thread.Sleep(TimeSpan.FromSeconds(1 << (attempt - 1)));

                // The last attempt goes to the original host in case the mirror is broken.
                string url = attempt == attempts - 1 && attempts > 1 ? file.Url : NetHooks.Rewrite(file.Url);
                try
                {
                    Task.Run(() => new WebDownload().DownloadFileAsync(file, url, CancellationToken.None))
                        .GetAwaiter().GetResult();
                    return;
                }
                catch (Exception ex)
                {
                    last = ex;
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            throw last ?? new IOException("Download failed: " + file.Url);
        }

        internal async Task DownloadFileAsync(DownloadFile file, string url, CancellationToken ct)
        {
            string? directoryName = Path.GetDirectoryName(file.Path);
            if (!string.IsNullOrEmpty(directoryName))
                Directory.CreateDirectory(directoryName);

            string partPath = file.Path + PartExtension;
            long metaSize = file.Size;

            long existing = 0;
            try
            {
                if (File.Exists(partPath))
                    existing = new FileInfo(partPath).Length;
            }
            catch (IOException)
            {
                existing = 0;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (existing > 0)
                request.Headers.Range = new RangeHeaderValue(existing, null);

            HttpResponseMessage response;
            using (var headersCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                headersCts.CancelAfter(HeadersTimeout);
                try
                {
                    response = await LibHttp.Files.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                        headersCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
                {
                    throw new TimeoutException("No response from " + url, ex);
                }
            }

            long? expectedTotal;
            using (response)
            {
                bool append = false;
                if (existing > 0 && response.StatusCode == HttpStatusCode.PartialContent)
                {
                    var range = response.Content.Headers.ContentRange;
                    if (range == null || range.From != existing)
                    {
                        tryDelete(partPath);
                        throw new IOException("Unexpected Content-Range from " + url);
                    }
                    append = true;
                }
                else if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    // Part is bigger than the file or the server changed it: start over next attempt.
                    tryDelete(partPath);
                    throw new IOException("Range not satisfiable: " + url);
                }

                response.EnsureSuccessStatusCode();

                long startAt = append ? existing : 0;
                long? contentLength = response.Content.Headers.ContentLength;
                expectedTotal = append ? response.Content.Headers.ContentRange?.Length : contentLength;
                if (expectedTotal == null && contentLength != null)
                    expectedTotal = startAt + contentLength;

                long totalForProgress = expectedTotal ?? (metaSize > 0 ? metaSize : 0);

                using var idleCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                using var webStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                using (var fileStream = new FileStream(partPath, append ? FileMode.Append : FileMode.Create,
                    FileAccess.Write, FileShare.None, DefaultBufferSize, FileOptions.Asynchronous))
                {
                    long received = startAt;
                    if (startAt > 0)
                        reportProgress(file, totalForProgress, startAt, received);

                    byte[] buffer = new byte[DefaultBufferSize];
                    while (true)
                    {
                        idleCts.CancelAfter(IdleTimeout);
                        int length;
                        try
                        {
                            length = await webStream.ReadAsync(buffer.AsMemory(0, buffer.Length), idleCts.Token)
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
                        {
                            throw new TimeoutException("Download stalled: " + url, ex);
                        }

                        if (length <= 0)
                            break;

                        await fileStream.WriteAsync(buffer.AsMemory(0, length), ct).ConfigureAwait(false);
                        received += length;
                        reportProgress(file, totalForProgress, length, received);
                    }

                    await fileStream.FlushAsync(ct).ConfigureAwait(false);
                }
            }

            verifyPart(file, partPath, url, expectedTotal, metaSize);

            File.Move(partPath, file.Path, true);
        }

        private static void verifyPart(DownloadFile file, string partPath, string url, long? expectedTotal, long metaSize)
        {
            long length = new FileInfo(partPath).Length;

            if (expectedTotal.HasValue && length != expectedTotal.Value)
            {
                // A shorter part is kept for resume, a longer one is garbage.
                if (length > expectedTotal.Value)
                    tryDelete(partPath);
                throw new IOException($"Incomplete download {url}: {length}/{expectedTotal.Value} bytes");
            }

            if (!string.IsNullOrEmpty(file.Hash))
            {
                if (!IOUtil.CheckSHA1(partPath, file.Hash.ToLowerInvariant()))
                {
                    tryDelete(partPath);
                    throw new InvalidDataException("SHA1 mismatch: " + url);
                }
            }
            else if (!expectedTotal.HasValue && metaSize > 0 && length != metaSize)
            {
                tryDelete(partPath);
                throw new InvalidDataException($"Size mismatch {url}: {length}/{metaSize} bytes");
            }
        }

        private void reportProgress(DownloadFile file, long total, long progressed, long received)
        {
            int percent = total > 0 ? (int)((float)received / total * 100) : 0;

            FileDownloadProgressChanged?.Invoke(this,
                new DownloadFileProgress(file, total, progressed, received, percent));

            if (total > 0)
                DownloadProgressChangedEvent?.Invoke(this, new ProgressChangedEventArgs(percent, null));
        }

        private static void tryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}
