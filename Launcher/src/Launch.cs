using BlockifyLib.Launcher.Minecraft;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BlockifyLib.Launcher.src
{

    public class Launch
    {
        private const int DefaultServerPort = 25565;

        public static readonly string SupportVersion = "1.17.1";
        public readonly static string[] DefaultJavaParameter =
            {
                "-XX:+UnlockExperimentalVMOptions",
                "-XX:+UseG1GC",
                "-XX:G1NewSizePercent=20",
                "-XX:G1ReservePercent=20",
                "-XX:MaxGCPauseMillis=50",
                "-XX:G1HeapRegionSize=16M",
                "-Dlog4j2.formatMsgNoLookups=true"
                // "-Xss1M"
            };

        private readonly MinecraftPath minecraftPath;
        private readonly LaunchOption launchOption;

        public Launch(LaunchOption option)
        {
            this.launchOption = option;
            this.minecraftPath = option.GetMinecraftPath();
        }

        // Resolve the game directory: a per-instance folder when set (modpacks),
        // otherwise the shared .minecraft base path.
        private string GameDir()
        {
            string dir = string.IsNullOrEmpty(launchOption.GameDirectory)
                ? minecraftPath.BasePath : launchOption.GameDirectory!;
            try { if (!Directory.Exists(dir)) Directory.CreateDirectory(dir); } catch { }
            return dir;
        }

        public Process GetProcess()
        {
            Process mc = new Process();
            mc.StartInfo.FileName =
                useNotNull(launchOption.GetStartVersion().JavaBinaryPath, launchOption.GetJavaPath()) ?? "";

            // One entry per real argument: .NET quotes and escapes each of them itself.
            foreach (string arg in CreateArgList())
                mc.StartInfo.ArgumentList.Add(arg);

            mc.StartInfo.WorkingDirectory = GameDir();

            return mc;
        }

        private string createClassPath(Version.Version version)
        {
            List<string> classpath =
                new List<string>(version.Libraries?.Length ?? 1);

            if (version.Libraries != null)
                classpath.AddRange(version.Libraries
                    .Where(lib => lib.IsRequire && !lib.IsNative && !string.IsNullOrEmpty(lib.Path))
                    .Select(lib => Path.GetFullPath(Path.Combine(minecraftPath.Library, lib.Path!))));

            if (!string.IsNullOrEmpty(version.Jar))
                classpath.Add(minecraftPath.GetVersionJarPath(version.Jar));

            // Raw value: the whole classpath is one argument, no quotes inside.
            return string.Join(Path.PathSeparator.ToString(), classpath.Select(p => Path.GetFullPath(p)));
        }

        private string createNativePath(Version.Version version)
        {
            Native native = new Native(minecraftPath, version);
            native.CleanNatives();
            return native.ExtractNatives();
        }

        // Command-line form: one quoted entry per argument, so string.Join(" ", ...) is a valid command line.
        public string[] CreateArg() =>
            CreateArgList().Select(a => QuoteArgument(a)).ToArray();

        // Raw arguments for ProcessStartInfo.ArgumentList: one entry per argument, no quoting.
        public List<string> CreateArgList()
        {
            Version.Version version = launchOption.GetStartVersion();
            List<string> args = new List<string>();

            string classpath = createClassPath(version);
            string nativePath = createNativePath(version);
            Minecraft.Auth.Session session = launchOption.GetSession();

            var argDict = new Dictionary<string, string?>
            {
                { "library_directory", minecraftPath.Library },
                { "natives_directory", nativePath },
                { "launcher_name", useNotNull(launchOption.GameLauncherName, "minecraft-launcher") },
                { "launcher_version", useNotNull(launchOption.GameLauncherVersion, "2") },
                { "classpath_separator", Path.PathSeparator.ToString() },
                { "classpath", classpath },

                { "auth_player_name" , session.Username },
                { "version_name"     , version.id },
                { "game_directory"   , GameDir() },
                { "assets_root"      , minecraftPath.Assets },
                { "assets_index_name", version.AssetId ?? "legacy" },
                { "auth_uuid"        , session.UUID },
                { "auth_access_token", session.AccessToken },
                { "user_properties"  , "{}" },
                { "auth_xuid"        , session.Xuid ?? "xuid" },
                { "clientid"         , launchOption.ClientId ?? "clientId" },
                { "user_type"        , session.UserType ?? "Mojang" },
                { "game_assets"      , minecraftPath.GetAssetLegacyPath(version.AssetId ?? "legacy") },
                { "auth_session"     , session.AccessToken },
                { "version_type"     , useNotNull(launchOption.VersionType, version.TypeStr) }
            };

            if (version.JvmArguments != null)
                args.AddRange(Mapper.MapInterpolation(version.JvmArguments, argDict, false));

            // Heap size always comes from the launch option; custom JVM arguments only add to it.
            if (launchOption.MaximumRamMb > 0)
                args.Add("-Xmx" + launchOption.MaximumRamMb + "m");

            if (launchOption.MinimumRamMb > 0)
                args.Add("-Xms" + launchOption.MinimumRamMb + "m");

            if (launchOption.JVMArguments != null)
            {
                bool ownHeap = launchOption.MaximumRamMb > 0 || launchOption.MinimumRamMb > 0;
                args.AddRange(normalizeUserArgs(launchOption.JVMArguments).Where(a => !string.IsNullOrWhiteSpace(a)
                    && !(ownHeap && (a.StartsWith("-Xmx") || a.StartsWith("-Xms")))));
                if (!args.Any(a => a.StartsWith("-Dlog4j2.formatMsgNoLookups")))
                    args.Add("-Dlog4j2.formatMsgNoLookups=true");
            }
            else
                args.AddRange(DefaultJavaParameter);

            if (version.JvmArguments == null)
            {
                args.Add("-Djava.library.path=" + nativePath);
                args.Add("-cp");
                args.Add(classpath);
            }

            if (!string.IsNullOrEmpty(launchOption.DockName))
                args.Add("-Xdock:name=" + launchOption.DockName);
            if (!string.IsNullOrEmpty(launchOption.DockIcon))
                args.Add("-Xdock:icon=" + launchOption.DockIcon);

            var loggingArgument = version.LoggingClient?.Argument;
            if (!string.IsNullOrEmpty(loggingArgument))
                args.Add(Mapper.Interpolation(loggingArgument, new Dictionary<string, string?>()
                {
                    { "path", minecraftPath.GetLogConfigFilePath(version.LoggingClient?.Id ?? version.id) }
                }, false));

            if (!string.IsNullOrEmpty(version.MainClass))
                args.Add(version.MainClass);

            if (version.GameArguments != null)
                args.AddRange(Mapper.MapInterpolation(version.GameArguments, argDict, false));
            else if (!string.IsNullOrEmpty(version.MinecraftArguments))
                args.AddRange(Mapper.MapInterpolation(
                    version.MinecraftArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries), argDict, false));

            if (!string.IsNullOrEmpty(launchOption.ServerIp))
            {
                args.Add("--quickPlayMultiplayer");
                if (launchOption.ServerPort != DefaultServerPort)
                    args.Add($"{launchOption.ServerIp}:{launchOption.ServerPort}");
                else
                    args.Add(launchOption.ServerIp);

                args.Add("--server");
                args.Add(launchOption.ServerIp);

                if (launchOption.ServerPort != DefaultServerPort)
                {
                    args.Add("--port");
                    args.Add(launchOption.ServerPort.ToString());
                }
            }

            if (launchOption.ScreenWidth > 0 && launchOption.ScreenHeight > 0)
            {
                args.Add("--width");
                args.Add(launchOption.ScreenWidth.ToString());
                args.Add("--height");
                args.Add(launchOption.ScreenHeight.ToString());
            }

            if (launchOption.FullScreen)
                args.Add("--fullscreen");

            return args;
        }

        // Custom JVM args used to be joined into one command line, so quotes typed by the user
        // grouped words. Keep that meaning: when any entry has a quote, re-split the joined line.
        private static IEnumerable<string> normalizeUserArgs(string[] userArgs)
        {
            if (!userArgs.Any(a => a != null && a.Contains('"')))
                return userArgs;

            return SplitCommandLine(string.Join(" ", userArgs));
        }

        // Splits a command line by the Windows (MSVC) rules: spaces separate, quotes group,
        // backslashes escape only before a quote.
        public static List<string> SplitCommandLine(string commandLine)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            bool hasToken = false;
            int i = 0;

            while (i < commandLine.Length)
            {
                char c = commandLine[i];
                if (c == '\\')
                {
                    int backslashes = 0;
                    while (i < commandLine.Length && commandLine[i] == '\\')
                    {
                        backslashes++;
                        i++;
                    }

                    if (i < commandLine.Length && commandLine[i] == '"')
                    {
                        current.Append('\\', backslashes / 2);
                        if (backslashes % 2 == 1)
                        {
                            current.Append('"');
                            i++;
                        }
                    }
                    else
                        current.Append('\\', backslashes);

                    hasToken = true;
                    continue;
                }

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    hasToken = true;
                }
                else if ((c == ' ' || c == '\t') && !inQuotes)
                {
                    if (hasToken)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                        hasToken = false;
                    }
                }
                else
                {
                    current.Append(c);
                    hasToken = true;
                }
                i++;
            }

            if (hasToken)
                result.Add(current.ToString());

            return result;
        }

        // Quotes one argument by the Windows (MSVC) rules; the reverse of SplitCommandLine.
        public static string QuoteArgument(string arg)
        {
            if (arg.Length > 0 && arg.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '"' }) < 0)
                return arg;

            var sb = new StringBuilder(arg.Length + 2);
            sb.Append('"');
            for (int i = 0; i < arg.Length; i++)
            {
                int backslashes = 0;
                while (i < arg.Length && arg[i] == '\\')
                {
                    backslashes++;
                    i++;
                }

                if (i == arg.Length)
                {
                    sb.Append('\\', backslashes * 2);
                    break;
                }

                if (arg[i] == '"')
                {
                    sb.Append('\\', backslashes * 2 + 1);
                    sb.Append('"');
                }
                else
                {
                    sb.Append('\\', backslashes);
                    sb.Append(arg[i]);
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private string? useNotNull(string? input1, string? input2)
        {
            if (string.IsNullOrEmpty(input1))
                return input2;
            else
                return input1;
        }
    }
}
