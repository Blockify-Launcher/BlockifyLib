using System.ComponentModel;
using System.IO;
using System.Net;

namespace BlockifyLib.Launcher
{
    public class WebDownload
    {
        private static int DefaultBufferSize = (int)Math.Pow(1024, 2);

        public event ProgressChangedEventHandler ProgressChangedEvent;

        public void FileDownload(string url, string path)
        {
            WebRequest req = WebRequest.CreateHttp(url);
            WebResponse response = req.GetResponse();

            long filesize = long.Parse(response.Headers.Get("Content-Length"));

            Stream? webStream = response.GetResponseStream();
            FileStream fileStream = File.Open(path, FileMode.Create);

            int bufferSize = DefaultBufferSize;
            byte[] buffer = new byte[bufferSize];
            int length = 0;

            int processedBytes = 0;
            while ((length = webStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, length);
                processedBytes += length;
                ProgressChanged(processedBytes, filesize);
            }

            buffer = null;
            webStream.Dispose();
            fileStream.Dispose();
        }

        private void ProgressChanged(long value, long max)
        {
            float percentage = (float)value / max * 100;
            ProgressChangedEventArgs Process = new ProgressChangedEventArgs((int)percentage, null);
            ProgressChangedEvent?.Invoke(this, Process);
        }
    }
}
