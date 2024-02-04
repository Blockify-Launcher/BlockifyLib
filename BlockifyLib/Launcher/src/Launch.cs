using Ionic.Zip;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BlockifyLib.BlockifyLib.Launcher.src
{

    public class Launch
    {
        public static string DefaultJavaParameter =
                "-XX:+UnlockExperimentalVMOptions " +
                "-XX:+UseG1GC " +
                "-XX:G1NewSizePercent=20 " +
                "-XX:G1ReservePercent=20 " +
                "-XX:MaxGCPauseMillis=50 " +
                "-XX:G1HeapRegionSize=16M";
        public static string SupportLaunchVersion = "1.15.1";

        public Launch(LaunchOption option)
        {
            option.CheckValid();
            LaunchOption = option;
        }

        public LaunchOption LaunchOption { get; private set; }

        public void Start() => GetProcess().Start();

        public Process GetProcess()
        {
            Native native = new Native(LaunchOption);
            native.CleanNatives();
            native.CreateNatives();

            string arg = makeArg();
            Process mc = new Process();
            mc.StartInfo.FileName = LaunchOption.JavaPath;
            mc.StartInfo.Arguments = arg;
            mc.StartInfo.WorkingDirectory = Minecraft.Minecraft.path;

            return mc;
        }

        string makeArg()
        {
            var profile = LaunchOption.StartProfile;

            var sb = new StringBuilder();

            if (LaunchOption.CustomJavaParameter == "")
                sb.Append(DefaultJavaParameter);
            else
                sb.Append(LaunchOption.CustomJavaParameter);

            sb.Append(" -Xmx" + LaunchOption.MaximumRamMb + "m");
            sb.Append(" -Djava.library.path=" + handleEmpty(LaunchOption.StartProfile.NativePath));
            sb.Append(" -cp ");

            foreach (var item in profile.Libraries)
            {
                if (!item.IsNative)
                    sb.Append(handleEmpty(item.Path.Replace("/", "\\")) + ";");
            }

            string mcjarid = profile.Jar;

            sb.Append(handleEmpty(Minecraft.Minecraft.Versions + mcjarid + "\\" + mcjarid + ".jar") + " ");
            sb.Append(LaunchOption.StartProfile.MainClass + "  ");

            Dictionary<string, string> argDicts = new Dictionary<string, string>()
            {
                { "${auth_player_name}",    LaunchOption.Session.Username },
                { "${version_name}",        LaunchOption.StartProfile.Id },
                { "${game_directory}",      Minecraft.Minecraft._Path },
                { "${assets_root}",         Minecraft.Minecraft._Assets },
                { "${assets_index_name}",   profile.AssetId },
                { "${auth_uuid}",           LaunchOption.Session.UUID },
                { "${auth_access_token}",   LaunchOption.Session.AccessToken },
                { "${user_properties}", "{}" },
                { "${user_type}", "Mojang" },
                { "${game_assets}",         Minecraft.Minecraft.AssetLegacy },
                { "${auth_session}",        LaunchOption.Session.AccessToken }
            };

            if (LaunchOption.LauncherName == "")
                argDicts.Add("${version_type}", profile.TypeStr);
            else
                argDicts.Add("${version_type}", LaunchOption.LauncherName);

            if (LaunchOption.StartProfile.GameArguments != null)
            {
                foreach (var item in LaunchOption.StartProfile.GameArguments)
                {
                    var argStr = item.ToString();

                    if (argStr[0] != '$')
                        sb.Append(argStr);
                    else
                    {
                        var argValue = "";
                        if (argDicts.TryGetValue(argStr, out argValue))
                            sb.Append(handleEmpty(argValue));
                        else
                            sb.Append(argStr);
                    }

                    sb.Append(" ");
                }
            }
            else
            {
                var gameArgBuilder = new StringBuilder(LaunchOption.StartProfile.MinecraftArguments);

                foreach (var item in argDicts)
                {
                    gameArgBuilder.Replace(item.Key, handleEmpty(item.Value));
                }

                sb.Append(gameArgBuilder.ToString());
            }

            if (LaunchOption.ServerIp != "")
                sb.Append(" --server " + LaunchOption.ServerIp);

            if (LaunchOption.ScreenWidth > 0 && LaunchOption.ScreenHeight > 0)
            {
                sb.Append(" --width ");
                sb.Append(LaunchOption.ScreenWidth);
                sb.Append(" --height ");
                sb.Append(LaunchOption.ScreenHeight);
            }

            return sb.ToString();
        }

        private void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, true);
        }

        private string handleEmpty(string input)
        {
            if (input.Contains(" "))
                return "\"" + input + "\"";
            else
                return input;
        }
    }
}
