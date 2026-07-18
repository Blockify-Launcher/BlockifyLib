using Newtonsoft.Json;
using System.Security.RightsManagement;

namespace BlockifyLib.Launcher.Minecraft.Auth
{
    public enum UserType
    {
        Mojang,
        Microsoft, 
        Offline
    }

    public class SessionStruct
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("username")]
        public string? Username { get; set; }
        [JsonProperty("session")]
        public string? AccessToken { get; set; }
        [JsonProperty("uuid")]
        public string? UUID { get; set; }
        [JsonProperty("clientToken")]
        public string? ClientToken { get; set; }
        public string? Xuid { get; set; }
        public string? UserType { get; set; }
    }

    public class Session : SessionStruct
    {
        public Session() { }

        public Session(string? username, string? accessToken, string? uuid)
        {
            Username = username;
            AccessToken = accessToken;
            UUID = uuid;
        }

        public bool CheckIsValid()
        {
            return !string.IsNullOrEmpty(Username)
                && !string.IsNullOrEmpty(AccessToken)
                && !string.IsNullOrEmpty(UUID);
        }

        private static string GenerateUniqueId(string username) =>
            $"{username}_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

        // Standard offline UUID: name-based (v3) MD5 of "OfflinePlayer:<name>", 32 hex, no dashes.
        // A real UUID is required — some mods (e.g. Essential) call getId() and crash on a bogus one.
        private static string OfflineUuid(string username)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var data = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes("OfflinePlayer:" + username));
            data[6] = (byte)((data[6] & 0x0f) | 0x30); // version 3
            data[8] = (byte)((data[8] & 0x3f) | 0x80); // IETF variant
            return Convert.ToHexString(data).ToLowerInvariant();
        }

        public static Session GetOfflineSession(string username)
        {
            return new Session
            {
                Id = GenerateUniqueId(username),
                Username = username,
                AccessToken = "0",           // no online token for offline play
                UUID = OfflineUuid(username),
                UserType = "legacy",
                ClientToken = null
            };
        }

        public static Session CreateOfflineSession(string username)
        {
            return new Session
            {
                Id = GenerateUniqueId(username),
                Username = username,
                AccessToken = "access_token",
                UUID = Guid.NewGuid().ToString().Replace("-", ""),
                UserType = "msa",
                ClientToken = null
            };
        }
    }
}
