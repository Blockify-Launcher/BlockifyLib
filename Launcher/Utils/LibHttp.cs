using System.Net;
using System.Net.Http;

namespace BlockifyLib.Launcher.Utils
{
    // Shared HttpClients of the library. Both honour NetHooks.Proxy or the system proxy.
    internal static class LibHttp
    {
        public static readonly TimeSpan DefaultApiTimeout = TimeSpan.FromSeconds(20);
        private const string UserAgent = "BlockifyLauncher/1.0";

        private static readonly object sync = new object();
        private static HttpClient? api, files;
        private static IWebProxy? apiProxy, filesProxy;

        // Small JSON requests: gzip on, callers pass their own timeout.
        public static HttpClient Api
        {
            get
            {
                var proxy = NetHooks.Proxy;
                lock (sync)
                {
                    if (api == null || !ReferenceEquals(apiProxy, proxy))
                    {
                        api = create(proxy, true);
                        apiProxy = proxy;
                    }
                    return api;
                }
            }
        }

        // Game files: no overall timeout (big files), the downloader watches for stalls itself.
        public static HttpClient Files
        {
            get
            {
                var proxy = NetHooks.Proxy;
                lock (sync)
                {
                    if (files == null || !ReferenceEquals(filesProxy, proxy))
                    {
                        files = create(proxy, false);
                        filesProxy = proxy;
                    }
                    return files;
                }
            }
        }

        private static HttpClient create(IWebProxy? proxy, bool decompress)
        {
            var handler = new SocketsHttpHandler
            {
                UseProxy = true,
                Proxy = proxy, // null = system proxy
                DefaultProxyCredentials = CredentialCache.DefaultCredentials,
                AutomaticDecompression = decompress ? DecompressionMethods.All : DecompressionMethods.None,
                ConnectTimeout = TimeSpan.FromSeconds(15),
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                MaxConnectionsPerServer = 16
            };

            var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            return client;
        }

        // GET as string. The rewritten (mirror) URL goes first, the original one is the fallback.
        public static async Task<string> GetStringAsync(string url, TimeSpan timeout,
            CancellationToken ct = default)
        {
            string target = NetHooks.Rewrite(url);
            try
            {
                return await getStringCore(target, timeout, ct).ConfigureAwait(false);
            }
            catch (Exception) when (target != url && !ct.IsCancellationRequested)
            {
                return await getStringCore(url, timeout, ct).ConfigureAwait(false);
            }
        }

        public static Task<string> GetStringAsync(string url) =>
            GetStringAsync(url, DefaultApiTimeout);

        public static string GetString(string url, TimeSpan timeout) =>
            Task.Run(() => GetStringAsync(url, timeout)).GetAwaiter().GetResult();

        public static string GetString(string url) =>
            GetString(url, DefaultApiTimeout);

        private static async Task<string> getStringCore(string url, TimeSpan timeout, CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            try
            {
                using var response = await Api.GetAsync(url, HttpCompletionOption.ResponseContentRead, cts.Token)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
            {
                throw new TimeoutException($"Request timed out after {timeout.TotalSeconds:0} s: {url}", ex);
            }
        }
    }
}
