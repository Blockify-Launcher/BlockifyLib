namespace BlockifyLib.Launcher.XboxAuthNet.Game.Authenticators;

public interface IAuthenticator
{
    ValueTask ExecuteAsync(AuthenticateContext context);
}