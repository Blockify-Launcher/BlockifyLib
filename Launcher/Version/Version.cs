using BlockifyLib.Launcher.src;
using BlockifyLib.Launcher.Version.Func;

namespace BlockifyLib.Launcher.Version
{
    public class Version
    {
        public string id { get; set; }

        public bool IsInherited { get; set; }
        public string? ParentVersionId { get; set; }

        public string? AssetId { get; set; }
        public string? AssetUrl { get; set; }
        public string? AssetHash { get; set; }

        public string? JavaVersion { get; set; }
        public string? JavaBinaryPath { get; set; }
        public string? Jar { get; set; }
        public string? ClientDownloadUrl { get; set; }
        public string? ClientHash { get; set; }
        public string? MainClass { get; set; }
        public string? MinecraftArguments { get; set; }
        public string[]? GameArguments { get; set; }
        public string[]? JvmArguments { get; set; }
        public string? ReleaseTime { get; set; }
        public string? TypeStr { get; set; }

        public Library[]? Libraries { get; set; }
        public LogConfig? LoggingClient { get; set; }

        public void InheritFrom(Version vers)
        {
            if (nc(AssetId))
                AssetId = vers.AssetId;

            if (nc(AssetUrl))
                AssetUrl = vers.AssetUrl;

            if (nc(AssetHash))
                AssetHash = vers.AssetHash;

            if (nc(ClientDownloadUrl))
                ClientDownloadUrl = vers.ClientDownloadUrl;

            if (nc(ClientHash))
                ClientHash = vers.ClientHash;

            if (nc(MainClass))
                MainClass = vers.MainClass;

            if (nc(MinecraftArguments))
                MinecraftArguments = vers.MinecraftArguments;

            if (nc(JavaVersion))
                JavaVersion = vers.JavaVersion;

            if (LoggingClient == null)
                LoggingClient = vers.LoggingClient;

            if (vers.Libraries != null)
                if (Libraries != null)
                    Libraries = Libraries.Concat(vers.Libraries).ToArray();
                else
                    Libraries = vers.Libraries;

            if (vers.GameArguments != null)
                if (GameArguments != null)
                    GameArguments = vers.GameArguments.Concat(GameArguments).ToArray();
                else
                    GameArguments = vers.GameArguments;

            if (vers.JvmArguments != null)
                if (JvmArguments != null)
                    JvmArguments = vers.JvmArguments.Concat(JvmArguments).ToArray();
                else
                    JvmArguments = vers.JvmArguments;
        }

        public ProfileConverter.VersionType Type { get; set; }
            = ProfileConverter.VersionType.Custom;

        public Version(string id) =>
            this.id = id;

        public static bool nc(string str) =>
            string.IsNullOrEmpty(str);

        public override string ToString() =>
            this.id;

    }
}
