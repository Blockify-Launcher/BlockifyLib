using System.Diagnostics;
using System.Net;

namespace BlockifyLib.Launcher.Utils
{
    /// <summary>
    /// Network hooks set by the host app once at startup.
    /// null means default behaviour: system proxy and URLs as they are.
    /// </summary>
    public static class NetHooks
    {
        // Maps an official URL to a mirror (or returns it unchanged).
        public static Func<string, string>? UrlRewriter { get; set; }

        // Explicit proxy for every request of the library; null = system proxy.
        public static IWebProxy? Proxy { get; set; }

        // Applies UrlRewriter; a broken rewriter never breaks the download.
        public static string Rewrite(string url)
        {
            var rewriter = UrlRewriter;
            if (rewriter == null || string.IsNullOrEmpty(url))
                return url;

            try
            {
                string result = rewriter(url);
                return string.IsNullOrEmpty(result) ? url : result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return url;
            }
        }
    }
}
