using BlockifyLib.Launcher.XboxAuthNet.Game.Authenticators;
using Microsoft.Identity.Client;

namespace BlockifyLib.Launcher.XboxAuthNet.Game.Msal;

public static class Extensions
{
    public static void AddMsalOAuth(
        this ICompositeAuthenticator self,
        IPublicClientApplication app,
        Func<MsalOAuthBuilder, IAuthenticator> builderInvoker)
    {
        var builder = new MsalOAuthBuilder(app);
        var authenticator = builderInvoker.Invoke(builder);
        self.AddAuthenticatorWithoutValidator(authenticator);
    }
}