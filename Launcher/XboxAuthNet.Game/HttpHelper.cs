using System.Net.Http;

namespace BlockifyLib.Launcher.XboxAuthNet.Game;

public static class HttpHelper
{
    public static Lazy<HttpClient> DefaultHttpClient
        = new Lazy<HttpClient>(() => new HttpClient());
}
