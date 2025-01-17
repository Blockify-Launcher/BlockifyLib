namespace BlockifyLib.Launcher.Bedrock.Auth.Sessions;

public class BESession
{
    public BEToken[]? Tokens { get; set; }

    public bool Validate()
    {
        return (Tokens != null)
            && (Tokens.Length > 0);
    }
}