using BlockifyLib.Launcher.Minecraft;
using BlockifyLib.Launcher.Minecraft.Auth;

namespace BlockifyLib.Launcher.src
{
    public class LaunchOption
    {
        public MinecraftPath? Path { get; set; }
        public Version.Version? StartVersion { get; set; }
        public Session? Session { get; set; }

        public string? JavaVersion { get; set; }
        public string? JavaPath { get; set; }
        public int MaximumRamMb { get; set; } = 1024;
        public int MinimumRamMb { get; set; }
        public string[]? JVMArguments { get; set; }

        public string? DockName { get; set; }
        public string? DockIcon { get; set; }

        public string? ServerIp { get; set; }
        public int ServerPort { get; set; } = 25565;

        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public bool FullScreen { get; set; }

        public string? ClientId { get; set; }
        public string? VersionType { get; set; }
        public string? GameLauncherName { get; set; }
        public string? GameLauncherVersion { get; set; }

        internal MinecraftPath GetMinecraftPath() => Path!;
        internal Version.Version GetStartVersion() => StartVersion!;
        internal Session GetSession() => Session!;
        internal string GetJavaPath() => JavaPath!;
    }

}
