using BlockifyLib.Launcher.XboxAuthNet.Game.Authenticators;
using BlockifyLib.Launcher.XboxAuthNet.Game.SessionStorages;
using BlockifyLib.Launcher.Microsoft.Sessions;

namespace BlockifyLib.Launcher.Microsoft.Auth;

public class JETokenValidator : SessionValidator<JEToken>
{
    public JETokenValidator(ISessionSource<JEToken> sessionSource)
    : base(sessionSource)
    {
        
    }

    protected override ValueTask<bool> Validate(AuthenticateContext context, JEToken token)
    {
        var valid = (token != null && token.Validate());
        context.Logger.LogJETokenValidator(valid);
        return new ValueTask<bool>(valid);
    }
}