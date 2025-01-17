using BlockifyLib.Launcher.XboxAuthNet.Game.SessionStorages;

namespace BlockifyLib.Launcher.Microsoft.Sessions;

public class JETokenSource : SessionFromStorage<JEToken>
{
    private static JETokenSource? _sessionSource;
    public static JETokenSource Default => _sessionSource ??= new();

    public static string KeyName { get; } = "JEToken";
    public JETokenSource() : base(KeyName)
    {

    }
}