using BlockifyLib.Launcher.XboxAuthNet.Game;
using BlockifyLib.Launcher.XboxAuthNet.Game.OAuth;
using BlockifyLib.Launcher.XboxAuthNet.Game.XboxAuth;
using BlockifyLib.Launcher.XboxAuthNet.Game.Authenticators;
using BlockifyLib.Launcher.Microsoft.Sessions;
using BlockifyLib.Launcher.Microsoft.Auth;
using BlockifyLib.Launcher.XboxAuthNet.Game.Accounts;
using BlockifyLib.Launcher.Minecraft.Auth;

namespace BlockifyLib.Launcher.Microsoft;

public static class Extensions
{
    public static void AddMicrosoftOAuthForJE(
        this ICompositeAuthenticator self,
        Func<MicrosoftOAuthBuilder, IAuthenticator> builderInvoker) =>
        self.AddMicrosoftOAuth(JELoginHandler.DefaultMicrosoftOAuthClientInfo, builderInvoker);

    public static void AddForceMicrosoftOAuthForJE(
        this ICompositeAuthenticator self,
        Func<MicrosoftOAuthBuilder, IAuthenticator> builderInvoker) =>
        self.AddForceMicrosoftOAuth(JELoginHandler.DefaultMicrosoftOAuthClientInfo, builderInvoker);

    public static void AddXboxAuthForJE(
        this ICompositeAuthenticator self,
        Func<XboxAuthBuilder, IAuthenticator> builderInvoker) =>
        self.AddXboxAuth(builder => 
        {
            builder.WithRelyingParty(JELoginHandler.RelyingParty);
            return builderInvoker(builder);
        });

    public static void AddForceXboxAuthForJE(
        this ICompositeAuthenticator self,
        Func<XboxAuthBuilder, IAuthenticator> builderInvoker) =>
        self.AddForceXboxAuth(builder =>
        {
            builder.WithRelyingParty(JELoginHandler.RelyingParty);
            return builderInvoker(builder);
        });

    public static void AddJEAuthenticator(this ICompositeAuthenticator self) =>
        self.AddJEAuthenticator(builder => builder.Build());

    public static void AddJEAuthenticator(
        this ICompositeAuthenticator self,
        Func<JEAuthenticatorBuilder, IAuthenticator> builderInvoker)
    {
        var builder = new JEAuthenticatorBuilder();
        var authenticator = builderInvoker.Invoke(builder);
        self.AddAuthenticator(builder.TokenValidator(), authenticator);
    }

    public static void AddForceJEAuthenticator(this ICompositeAuthenticator self) =>
        self.AddForceJEAuthenticator(builder => builder.Build());

    public static void AddForceJEAuthenticator(
        this ICompositeAuthenticator self,
        Func<JEAuthenticatorBuilder, IAuthenticator> builderInvoker)
    {
        var builder = new JEAuthenticatorBuilder();
        var authenticator = builderInvoker.Invoke(builder);
        self.AddAuthenticator(StaticValidator.Invalid, authenticator);
    }

    public static void AddJESignout(this ICompositeAuthenticator self)
    {
        self.AddSessionCleaner(JETokenSource.Default);
        self.AddSessionCleaner(JEProfileSource.Default);
    }

    public static JEGameAccount GetJEAccountByUsername(this XboxGameAccountCollection self, string username)
    {
        return (JEGameAccount)self.First(account =>
        {
            if (account is JEGameAccount jeAccount)
            {
                return jeAccount.Profile?.Username == username;
            }
            else
            {
                return false;
            }
        });
    }

    public static async Task<Session> ExecuteForLauncherAsync(this NestedAuthenticator self)
    {
        var session = await self.ExecuteAsync();
        var account = JEGameAccount.FromSessionStorage(session);
        return account.ToLauncherSession();
    }
}