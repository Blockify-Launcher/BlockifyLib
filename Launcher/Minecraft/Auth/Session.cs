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

        public static Session GetOfflineSession(string username)
        {
            return new Session
            {
                Username = username,
                AccessToken = "access_token",
                UUID = "user_uuid",
                UserType = "Mojang",
                ClientToken = null
            };
        }

        public static Session CreateOfflineSession(string username)
        {
            return new Session
            {
                Username = username,
                AccessToken = "access_token",
                UUID = Guid.NewGuid().ToString().Replace("-", ""),
                UserType = "msa",
                ClientToken = null
            };
        }
    }
}
