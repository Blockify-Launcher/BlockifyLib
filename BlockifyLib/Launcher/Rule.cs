using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BlockifyLib.BlockifyLib.Launcher
{
    public class Rule
    {
        static Rule()
        {
            OSName = getOSName();

            if (Environment.Is64BitOperatingSystem)
                Arch = "64";
            else
                Arch = "32";
        }

        public static string OSName { get; private set; }
        public static string Arch { get; private set; }

        private static string getOSName()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    return "osx";
                case PlatformID.Unix:
                    return "linux";
                default:
                    return "windows";
            }
        }

        public bool CheckOSRequire(JArray arr)
        {
            bool require = true;

            foreach (JObject job in arr)
            {
                bool action = true;                     // true : "allow", false : "disallow"
                bool containCurrentOS = true;           // if 'os' JArray contains current os name

                foreach (var item in job)
                {
                    if (item.Key == "action")           // action
                        action = (item.Value.ToString() == "allow" ? true : false);

                    else if (item.Key == "os")          // os (containCurrentOS)
                        containCurrentOS = checkOSContains((JObject)item.Value);

                    else if (item.Key == "features")    // etc
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
            foreach (var os in job)
                if (os.Key == "name" && os.Value.ToString() == OSName)
                    return true;
            return false;
        }
    }
}
