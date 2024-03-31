using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;

namespace BlockifyLib.Launcher
{
    public static class Rule
    {
        static Rule()
        {
            OSName = getOSName();

            if (Environment.Is64BitOperatingSystem)
                Arch = "64";
            else
                Arch = "32";
        }

        public static readonly string Windows = "windows";
        public static readonly string OSX = "osx";
        public static readonly string Linux = "linux";

        public static string OSName { get; private set; }
        public static string Arch { get; private set; }

        private static string getOSName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OSX;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Windows;
            else
                return Linux;
        }

        public static bool CheckOSRequire(JArray arr)
        {
            bool require = true;

            foreach (JObject item in arr)
            {
                if (item == null)
                    continue;

                bool action = true;                     // true : "allow", false : "disallow"
                bool containCurrentOS = true;           // if 'os' JArray contains current os name

                foreach (var __item in item)
                {
                    if (__item.Key == "action")           // action
                        action = __item.Value.ToString() == "allow" ? true : false;

                    else if (__item.Key == "os")          // os (containCurrentOS)
                        containCurrentOS = checkOSContains((JObject)__item.Value);

                    else if (__item.Key == "features")    // etc
                        return false;
                }

                if (!action && containCurrentOS)
                    require = false;
                else if (action && containCurrentOS)
                    require = true;
                else if (action && !containCurrentOS)
                    require = false;
            }

            return require;
        }

        static bool checkOSContains(JObject job)
        {
            if (job == null)
                return false;
            foreach (var os in job)
                if (os.Key == "name" && os.Value.ToString() == OSName)
                    return true;
            return false;
        }
    }
}
