using BlockifyLib.Launcher.XboxAuthNet.Game.Accounts;
using BlockifyLib.Launcher.XboxAuthNet.Game.SessionStorages;

namespace BlockifyLib.Launcher.Bedrock.Auth.Sessions;

public class BEGameAccount : XboxGameAccount
{
    public new static BEGameAccount FromSessionStorage(ISessionStorage sessionStorage) =>
        new BEGameAccount(sessionStorage);

    public BEGameAccount(ISessionStorage sessionStorage) : base(sessionStorage)
    {

    }

    public BESession? Session => BESessionSource.Default.Get(SessionStorage);
}