using Microsoft.Extensions.Logging;
using BlockifyLib.Launcher.XboxAuthNet.Game.SessionStorages;
using System.Net.Http;

namespace BlockifyLib.Launcher.XboxAuthNet.Game.Authenticators;

public class AuthenticateContext
{
    public AuthenticateContext(
        ISessionStorage sessionStorage, 
        HttpClient httpClient,
        CancellationToken cancellationToken,
        ILogger logger) =>
        (CancellationToken, SessionStorage, HttpClient, Logger) = 
        (cancellationToken, sessionStorage, httpClient, logger);

    public CancellationToken CancellationToken { get; }
    public ISessionStorage SessionStorage { get; }
    public HttpClient HttpClient { get; }
    public ILogger Logger { get; }
}