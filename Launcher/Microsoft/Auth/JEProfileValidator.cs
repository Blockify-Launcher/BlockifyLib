using BlockifyLib.Launcher.XboxAuthNet.Game.Authenticators;
using BlockifyLib.Launcher.XboxAuthNet.Game.SessionStorages;
using BlockifyLib.Launcher.Microsoft.Sessions;

namespace BlockifyLib.Launcher.Microsoft.Auth;

public class JEProfileValidator : SessionValidator<JEProfile>
{
    public JEProfileValidator(ISessionSource<JEProfile> sessionSource)
     : base(sessionSource)
    {

    }

    protected override ValueTask<bool> Validate(AuthenticateContext context, JEProfile profile)
    {
        var isValid = (
            !string.IsNullOrEmpty(profile.Username) && 
            !string.IsNullOrEmpty(profile.UUID));
        context.Logger.LogJEProfileValidator(isValid);
        return new ValueTask<bool>(isValid);
    }
}