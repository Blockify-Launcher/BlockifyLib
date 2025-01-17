using Microsoft.Extensions.Logging;
using BlockifyLib.Launcher.XboxAuthNet.Game.Accounts;
using BlockifyLib.Launcher.XboxAuthNet.Game.Authenticators;
using BlockifyLib.Launcher.XboxAuthNet.Game.SessionStorages;
using System.Net.Http;

namespace BlockifyLib.Launcher.XboxAuthNet.Game;

public class XboxGameLoginHandler
{
    private readonly ILogger _logger;
    protected readonly HttpClient HttpClient;
    public IXboxGameAccountManager AccountManager { get; }

    public XboxGameLoginHandler(LoginHandlerParameters parameters)
    {
        _logger = parameters.Logger;
        HttpClient = parameters.HttpClient;
        AccountManager = parameters.AccountManager;
    }

    public NestedAuthenticator CreateAuthenticator(
        IXboxGameAccount account,
        CancellationToken cancellationToken) =>
        CreateAuthenticator(account.SessionStorage, cancellationToken);

    public NestedAuthenticator CreateAuthenticator(
        ISessionStorage sessionStorage,
        CancellationToken cancellationToken)
    {
        var authenticator = new NestedAuthenticator();
        authenticator.Context = createContext(sessionStorage, cancellationToken);
        authenticator.AddPostAuthenticator(LastAccessLogger.Default);
        authenticator.AddPostAuthenticator(new AccountSaver(AccountManager));
        return authenticator;
    }

    private AuthenticateContext createContext(
        ISessionStorage sessionStorage,
        CancellationToken cancellationToken)
    {
        return new AuthenticateContext(
            sessionStorage,
            HttpClient,
            cancellationToken,
            _logger);
    }

    public NestedAuthenticator CreateAuthenticatorWithDefaultAccount(
        CancellationToken cancellationToken = default) =>
        CreateAuthenticator(AccountManager.GetDefaultAccount(), cancellationToken);

    public NestedAuthenticator CreateAuthenticatorWithNewAccount(
        CancellationToken cancellationToken = default) =>
        CreateAuthenticator(AccountManager.NewAccount(), cancellationToken);
}