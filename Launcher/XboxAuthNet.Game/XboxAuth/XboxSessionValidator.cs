using BlockifyLib.Launcher.XboxAuthNet.Game.Authenticators;
using BlockifyLib.Launcher.XboxAuthNet.Game.SessionStorages;

namespace BlockifyLib.Launcher.XboxAuthNet.Game.XboxAuth;

public class XboxSessionValidator : SessionValidator<XboxAuthTokens>
{
    public XboxSessionValidator(ISessionSource<XboxAuthTokens> sessionSource)
    : base(sessionSource)
    {
        
    }

    protected override ValueTask<bool> Validate(AuthenticateContext context, XboxAuthTokens session)
    {
        var result = session?.XstsToken?.Validate() ?? false;
        context.Logger.LogXboxValidation(result);
        return new ValueTask<bool>(result);
    }
}