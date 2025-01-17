using BlockifyLib.Launcher.XboxAuthNet.Game;
using BlockifyLib.Launcher.XboxAuthNet.Game.Accounts;
using BlockifyLib.Launcher.Microsoft.Sessions;
using System.IO;
using BlockifyLib.Launcher.Minecraft;

namespace BlockifyLib.Launcher.Microsoft;

public class JELoginHandlerBuilder : 
    XboxGameLoginHandlerBuilderBase<JELoginHandlerBuilder>
{
    public static JELoginHandler BuildDefault() => 
        new JELoginHandlerBuilder().Build();

    public JELoginHandlerBuilder WithAccountManager(string filePath)
    {
        return WithAccountManager(createAccountManager(filePath));
    }

    protected override IXboxGameAccountManager CreateDefaultAccountManager()
    {
        var defaultFilePath = Path.Combine(MinecraftPath.GetOSDefaultPath(), "cml_accounts.json");
        return createAccountManager(defaultFilePath);
    }

    private IXboxGameAccountManager createAccountManager(string filePath)
    {
        return new JsonXboxGameAccountManager(
            filePath, 
            JEGameAccount.FromSessionStorage, 
            JsonXboxGameAccountManager.DefaultSerializerOption);
    }

    public JELoginHandler Build()
    {
        var parameters = BuildParameters();
        return new JELoginHandler(parameters);
    }
}
