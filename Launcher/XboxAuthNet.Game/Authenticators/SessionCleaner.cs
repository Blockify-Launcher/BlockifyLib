using Microsoft.Extensions.Logging;
using BlockifyLib.Launcher.XboxAuthNet.Game.SessionStorages;

namespace BlockifyLib.Launcher.XboxAuthNet.Game.Authenticators;

public class SessionCleaner<T> : SessionAuthenticator<T>
{
    public SessionCleaner(ISessionSource<T> sessionSource)
     : base(sessionSource)
    {

    }

    protected override ValueTask<T?> Authenticate(AuthenticateContext context)
    {
        context.Logger.LogSessionCleaner(typeof(T).Name);
        return default;
    }
}