using BlockifyLib.Launcher.Downloader;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace BlockifyLib.Launcher.Utils
{
    internal class WebDownload
    {
        public static bool IgnoreProxy { get; set; } = true;

        public static int DefaultWebRequestTimeout { get; set; } = 20 * 1000;

        private class TimeoutWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = DefaultWebRequestTimeout;

                if (IgnoreProxy)
                {
                    w.Proxy = null;
                    this.Proxy = null;
                }

                return w;
            }
        }

        private static readonly int DefaultBufferSize = 1024 * 64;
        private readonly object locker = new object();

        internal event EventHandler<DownloadFileProgress>? FileDownloadProgressChanged;
        internal event ProgressChangedEventHandler? DownloadProgressChangedEvent;

        internal void DownloadFile(string url, string path)
        {
            WebResponse response = WebRequest.CreateHttp(url).GetResponse();
            long filesize = long.Parse(response.Headers.Get("Content-Length") ?? "0");

            Stream webStream = response.GetResponseStream();
            if (webStream == null)
                throw new NullReferenceException(nameof(webStream));

            FileStream fileStream = File.Open(path, FileMode.Create);

            int bufferSize = DefaultBufferSize;
            byte[] buffer = new byte[bufferSize];
            int length;

            bool fireEvent = filesize > DefaultBufferSize;
            int processedBytes = 0;

            while ((length = webStream.Read(buffer, 0, bufferSize)) > 0)
            {
                fileStream.Write(buffer, 0, length);

                if (fireEvent)
                {
                    processedBytes += length;
                    progressChanged(processedBytes, filesize);
                }
            }

            webStream.Dispose();
            fileStream.Dispose();
        }

        internal async Task DownloadFileAsync(DownloadFile file)
        {
            string? directoryName = Path.GetDirectoryName(file.Path);
            if (!string.IsNullOrEmpty(directoryName))
                Directory.CreateDirectory(directoryName);

            using (var wc = new TimeoutWebClient())
            {
                long lastBytes = 0;

                wc.DownloadProgressChanged += (s, e) =>
                {
                    lock (locker)
                    {
                        long progressedBytes = e.BytesReceived - lastBytes;
                        if (progressedBytes < 0)
                            return;

                        lastBytes = e.BytesReceived;

                        DownloadFileProgress progress = new DownloadFileProgress(
                            file, e.TotalBytesToReceive, progressedBytes, e.BytesReceived, e.ProgressPercentage);
                        FileDownloadProgressChanged?.Invoke(this, progress);
                    }
                };
                await wc.DownloadFileTaskAsync(file.Url, file.Path)
                    .ConfigureAwait(false);
            }
        }

        internal void DownloadFileLimit(string url, string path)
        {
            string? directoryName = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directoryName))
                Directory.CreateDirectory(directoryName);

            HttpWebRequest req = WebRequest.CreateHttp(url);
            req.Method = "GET";
            req.Timeout = 5000;
            req.ReadWriteTimeout = 5000;
            req.ContinueTimeout = 5000;
            WebResponse res = req.GetResponse();

            using var httpStream = res.GetResponseStream();
            if (httpStream == null)
                return;

            using var fs = File.OpenWrite(path);
            httpStream.CopyTo(fs);
        }

        private void progressChanged(long value, long max) =>
            DownloadProgressChangedEvent?.Invoke(this,
                new ProgressChangedEventArgs((int)((float)value / max * 100), null));
    }
}
