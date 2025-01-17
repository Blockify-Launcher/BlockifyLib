using BlockifyLib.Launcher.XboxAuthNet.Game.Authenticators;
using BlockifyLib.Launcher.XboxAuthNet.Game.SessionStorages;
using XboxAuthNet.OAuth;

namespace BlockifyLib.Launcher.XboxAuthNet.Game.OAuth;

public class MicrosoftOAuthValidator : SessionValidator<MicrosoftOAuthResponse>
{
    public MicrosoftOAuthValidator(
        ISessionSource<MicrosoftOAuthResponse> sessionSource)
        : base(sessionSource)
    {

    }
    
    protected override ValueTask<bool> Validate(AuthenticateContext context, MicrosoftOAuthResponse session)
    {
        var result = session.Validate();
        context.Logger.LogMicrosoftOAuthValidation(result);
        return new ValueTask<bool>(result);
    }
}