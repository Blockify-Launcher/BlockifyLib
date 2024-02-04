using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Controls;

namespace BlockifyLib.BlockifyLib.Launcher
{
    public enum LauncherFile
    {
        Library, Resource, Minecraft
    };

    public class DownloaderFileEventArgs : EventArgs
    {
        public LauncherFile file { get; set; }
        public string FileName { get; set; }
        public int TotalFileCount { get; set; }
        public int ProgressedFileCount { get; set; }
    }

    public delegate void DowloaderFile(DownloaderFileEventArgs e);
    
    public class DownloaderMinecraft
    {
        public event DowloaderFile                  ChangeFile;
        public event ProgressChangedEventHandler    ChangeProgress;
        public bool CheckHash = true;

        private Profile profile;
        private WebDownload web;

        public DownloaderMinecraft(Profile profile)
        {
            this.profile = profile;

            WebDownload web = new WebDownload();
            web.ProgressChangedEvent += WebDownloadProgress;
        }

        public void DownloadAll(bool resource = true)
        {
            DownloadLibraries();

            if (resource)
            {
                DownloadIndex();
                DownloadResource();
            }

            DownloadMinecraft();
        }

        public void DownloadLibraries()
        {
            int index = 0;
            foreach (var item in profile.Libraries)
            {
                try
                {
                    if (CheckDownloadRequireLibrary(item))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(item.Path));
                        web.FileDownload(item.Url, item.Path);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }

                Load(LauncherFile.Library, item.Name, profile.Libraries.Length, ++index); 
            }
        }

        private bool CheckDownloadRequireLibrary(Library lib)
        {
            return lib.IsRequire
                && lib.Path != ""
                && lib.Url != ""
                && !CheckFileValidation(lib.Path, lib.Hash);
        }

        public void DownloadIndex()
        {
            string path = Minecraft.Minecraft.Index + profile.AssetId + ".json";

            if (profile.AssetUrl != "" && !CheckFileValidation(path, profile.AssetHash))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                using (WebClient wc = new WebClient())
                    wc.DownloadFile(profile.AssetUrl, path);
            }
        }

        public void DownloadResource()
        {
            string indexpath = Minecraft.Minecraft.Index + profile.AssetId + ".json";
            if (!File.Exists(indexpath)) return;

            using (WebClient wc = new WebClient())
            {
                bool isVirtual = false;
                bool mapResource = false;

                string json = File.ReadAllText(indexpath);
                JObject index = JObject.Parse(json);

                string virtualValue = index["virtual"]?.ToString()?.ToLower();
                if (virtualValue != null && virtualValue == "true")
                    isVirtual = true;

                string mapResourceValue = index["map_to_resources"]?.ToString()?.ToLower();
                if (mapResourceValue != null && mapResourceValue == "true")
                    mapResource = true;

                JObject list = (JObject)index["objects"];
                int count = list.Count;
                int i = 0;

                foreach (var item in list)
                {
                    JToken job = item.Value;

                    string hash = job["hash"]?.ToString();
                    string hashName = hash.Substring(0, 2) + "\\" + hash;
                    string hashPath = Minecraft.Minecraft.AssetObject + hashName;
                    string hashUrl = "http://resources.download.minecraft.net/" + hashName;
                    Directory.CreateDirectory(Path.GetDirectoryName(hashPath));

                    if (!File.Exists(hashPath))
                        wc.DownloadFile(hashUrl, hashPath);

                    if (isVirtual)
                    {
                        string resPath = Minecraft.Minecraft.AssetLegacy + item.Key;

                        if (!File.Exists(resPath))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(resPath));
                            File.Copy(hashPath, resPath, true);
                        }
                    }

                    if (mapResource)
                    {
                        var resPath = Minecraft.Minecraft.Resource + item.Key;

                        if (!File.Exists(resPath))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(resPath));
                            File.Copy(hashPath, resPath, true);
                        }
                    }

                    Load(LauncherFile.Resource, profile.AssetId, count, ++i);
                }
            }
        }

        public void DownloadMinecraft()
        {
            if (profile.ClientDownloadUrl == "") return;

            Load(LauncherFile.Minecraft, profile.Jar, 1, 0);

            string id = profile.Jar;
            var path = Minecraft.Minecraft.Versions + id + "\\" + id + ".jar";
            if (!CheckFileValidation(path, profile.ClientHash))
            {
                Directory.CreateDirectory(Minecraft.Minecraft.Versions + id);
                web.FileDownload(profile.ClientDownloadUrl, path);
            }

            Load(LauncherFile.Minecraft, profile.Id, 1, 1);
        }

        private void Load(LauncherFile file, string name, int max, int value)
        {
            ChangeFile?.Invoke(new DownloaderFileEventArgs()
            {
                file = file,
                FileName = name,
                TotalFileCount = max,
                ProgressedFileCount = value
            });
        }

        private void WebDownloadProgress(object sender, ProgressChangedEventArgs e) => 
            ChangeProgress?.Invoke(this, e);

        private bool CheckFileValidation(string path, string hash) =>
            File.Exists(path) && CheckSHA1(path, hash);

        private bool CheckSHA1(string path, string compareHash)
        {
            try
            {
                if (!CheckHash)
                    return true;

                if (compareHash == null || compareHash == "")
                    return true;

                var fileHash = "";

                using (var file = File.OpenRead(path))
                using (var hasher = new System.Security.Cryptography.SHA1CryptoServiceProvider())
                {
                    var binaryHash = hasher.ComputeHash(file);
                    fileHash = BitConverter.ToString(binaryHash).Replace("-", "").ToLower();
                }

                return fileHash == compareHash;
            }
            catch
            {
                return false;
            }
        }
    }
}

