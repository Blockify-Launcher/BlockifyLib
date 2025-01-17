using BlockifyLib.Launcher.XboxAuthNet.Game.SessionStorages;

namespace BlockifyLib.Launcher.XboxAuthNet.Game.Authenticators;

public class StaticSessionAuthenticator<T> : SessionAuthenticator<T>
{
    private readonly T? _session;

    public StaticSessionAuthenticator(
        T? session,
        ISessionSource<T> sessionSource)
        : base(sessionSource) =>
        _session = session;

    protected override ValueTask<T?> Authenticate(AuthenticateContext context)
    {
        return new ValueTask<T?>(_session);
    }
}