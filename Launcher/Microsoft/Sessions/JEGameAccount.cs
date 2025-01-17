using BlockifyLib.Launcher.Minecraft.Auth;
using BlockifyLib.Launcher.XboxAuthNet.Game.Accounts;
using BlockifyLib.Launcher.XboxAuthNet.Game.SessionStorages;

namespace BlockifyLib.Launcher.Microsoft.Sessions;

public class JEGameAccount : XboxGameAccount
{
    public new static JEGameAccount FromSessionStorage(ISessionStorage sessionStorage)
    {
        return new JEGameAccount(sessionStorage);
    }

    public JEGameAccount(ISessionStorage sessionStorage) : base(sessionStorage)
    {

    }

    public JEProfile? Profile => JEProfileSource.Default.Get(SessionStorage);
    public JEToken? Token => JETokenSource.Default.Get(SessionStorage);

    public Session ToLauncherSession()
    {
        return new Session
        {
            Username = Profile?.Username,
            UUID = Profile?.UUID,
            AccessToken = Token?.AccessToken,
            UserType = "msa",
            Xuid = XboxTokens?.XstsToken?.XuiClaims?.XboxUserId
        };
    }
}