using BlockifyLib.Launcher.XboxAuthNet.Game.SessionStorages;

namespace BlockifyLib.Launcher.XboxAuthNet.Game.Accounts;

public interface IXboxGameAccount : IComparable
{
    string? Identifier { get; }
    ISessionStorage SessionStorage { get; }
}