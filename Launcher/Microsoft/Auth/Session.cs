using Newtonsoft.Json;

namespace BlockifyLib.Launcher.Microsoft.Auth
{
    public class Session
    {
        [JsonProperty("username")]
        public string? Username { get; set; }

        [JsonProperty("roles")]
        public string[]? Roles { get; set; }

        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string? TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("expires_on")]
        public DateTime ExpiresOn { get; set; }
    }
}
