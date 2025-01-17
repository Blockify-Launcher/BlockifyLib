using BlockifyLib.Launcher.XboxAuthNet.Game.Authenticators;
using BlockifyLib.Launcher.XboxAuthNet.Game.SessionStorages;
using XboxAuthNet.XboxLive;
using XboxAuthNet.XboxLive.Requests;

namespace BlockifyLib.Launcher.XboxAuthNet.Game.XboxAuth;

public class XboxXstsTokenAuth : SessionAuthenticator<XboxAuthTokens>
{
    private readonly string _relyingParty;

    public XboxXstsTokenAuth(
        string relyingParty,
        ISessionSource<XboxAuthTokens> sessionSource)
        : base(sessionSource) =>
        _relyingParty = relyingParty;

    protected override async ValueTask<XboxAuthTokens?> Authenticate(AuthenticateContext context)
    {
        context.Logger.LogXboxXstsTokenAuth(_relyingParty);

        var xboxTokens = GetSessionFromStorage() ?? new XboxAuthTokens();

        var xboxAuthClient = new XboxAuthClient(context.HttpClient);
        var xsts = await xboxAuthClient.RequestXsts(new XboxXstsRequest
        {
            UserToken = xboxTokens.UserToken?.Token,
            DeviceToken = xboxTokens.DeviceToken?.Token,
            TitleToken = xboxTokens.TitleToken?.Token,
            RelyingParty = _relyingParty,
        });
        
        xboxTokens.XstsToken = xsts;
        return xboxTokens;
    }
}