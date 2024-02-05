using BlockifyLib.Launcher;
using BlockifyLib.Launcher.Minecraft.Auth;

namespace BlockifyLib.Launcher.src
{
    public class LaunchOption
    {
        public string JavaPath { get; set; } = "";
        public int MaximumRamMb { get; set; } = 1024;
        public Version StartProfile { get; set; } = null;
        public Session Session { get; set; } = null;
        public string LauncherName { get; set; } = "";
        public string ServerIp { get; set; } = "";
        public string CustomJavaParameter { get; set; } = "";

        public int ScreenWidth { get; set; } = 0;
        public int ScreenHeight { get; set; } = 0;

        internal void CheckValid()
        {
            var exMsg = "";

            if (MaximumRamMb < 1)
                exMsg = "MaximumRamMb is too small.";

            if (StartProfile == null)
                exMsg = "StartProfile is null";

            if (Session == null)
                exMsg = "Session is null";

            if (LauncherName == null)
                LauncherName = "";

            else if (LauncherName.Contains(" "))
                exMsg = "Launcher Name must not contains Space.";

            if (ServerIp == null)
                ServerIp = "";

            if (CustomJavaParameter == null)
                CustomJavaParameter = "";

            if (ScreenWidth < 0 || ScreenHeight < 0)
                exMsg = "Screen Size must be greater than or equal to zero.";

            if (exMsg != "")
                throw new ArgumentException(exMsg);
        }
    }
}
