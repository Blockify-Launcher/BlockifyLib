using BlockifyLib.Launcher.XboxAuthNet.Game;
using BlockifyLib.Launcher.XboxAuthNet.Game.XboxAuth;
using BlockifyLib.Launcher.XboxAuthNet.Game.Authenticators;
using BlockifyLib.Launcher.Bedrock.Auth.Sessions;

namespace BlockifyLib.Launcher.Bedrock.Auth;

public static class Extensions
{
    public static void AddXboxAuthForBE(
        this ICompositeAuthenticator self,
        Func<XboxAuthBuilder, IAuthenticator> builderInvoker) =>
        self.AddXboxAuth(builder => 
        {
            builder.RelyingParty = BEAuthenticator.RelyingParty;
            return builderInvoker.Invoke(builder);
        });

    public static void AddForceXboxAuthForBE(
        this ICompositeAuthenticator self,
        Func<XboxAuthBuilder, IAuthenticator> builderInvoker) =>
        self.AddForceXboxAuth(builder =>
        {
            builder.RelyingParty = BEAuthenticator.RelyingParty;
            return builderInvoker.Invoke(builder);
        });

    public static void AddBEAuthenticator(this ICompositeAuthenticator self)
    {
        var authenticator = new BEAuthenticator(
            XboxSessionSource.Default,
            BESessionSource.Default);
        self.AddAuthenticatorWithoutValidator(authenticator);
    }
}