using BlockifyLib.Launcher.Minecraft.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace BlockifyLib.Launcher.Minecraft.Mojang.Launcher
{
    public class MojangAccount
    {
        [JsonProperty("accessToken")]
        public string? AccessToken { get; set; }

        [JsonProperty("accessTokenExpiresAt")]
        public DateTime? AccessTokenExpiresAt { get; set; }

        [JsonProperty("avatar")]
        public string? Avatar { get; set; }

        [JsonProperty("eligibleForMigration")]
        public bool EligibleForMigration { get; set; }

        [JsonProperty("hasMultipleProfiles")]
        public bool HasMultipleProfiles { get; set; }

        [JsonProperty("legacy")]
        public bool Legacy { get; set; }

        [JsonProperty("localId")]
        public string? LocalId { get; set; }

        [JsonProperty("minecraftProfile")]
        public JObject? MinecraftProfile { get; set; }

        public string? MinecraftProfileId
            => MinecraftProfile?["id"]?.ToString();

        public string? MinecraftProfileName
            => MinecraftProfile?["name"]?.ToString();

        [JsonProperty("persistent")]
        public bool Persistent { get; set; }

        [JsonProperty("remoteId")]
        public string? RemoteId { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("userProperites")]
        public List<object>? UserProperites { get; set; }

        [JsonProperty("username")]
        public string? Username { get; set; }

        public Session ToSession()
        {
            return new Session
            {
                Username = this.MinecraftProfileName,
                UUID = this.MinecraftProfileId,
                AccessToken = this.AccessToken
            };
        }
    }

    public class MojangLauncherAccounts
    {
        [JsonProperty("accounts")]
        public Dictionary<string, MojangAccount?>? Accounts { get; set; }

        [JsonProperty("ActiveAccountLocalId")]
        public string? ActiveAccountLocalId { get; set; }

        [JsonProperty("mojangClientToken")]
        public string? MojangClientToken { get; set; }

        public MojangAccount? GetActiveAccount()
        {
            if (string.IsNullOrEmpty(ActiveAccountLocalId))
                return null;

            MojangAccount? value = null;
            var result = Accounts?.TryGetValue(ActiveAccountLocalId, out value);
            if (result == null || result == false)
                return null;

            return value;
        }

        public void SaveTo(string path)
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            File.WriteAllText(path, json);
        }

        public static MojangLauncherAccounts? FromDefaultPath()
        {
            var path = Path.Combine(MinecraftPath.GetOSDefaultPath(), "launcher_accounts.json");
            return FromFile(path);
        }

        public static MojangLauncherAccounts? FromFile(string path)
        {
            var content = File.ReadAllText(path);
            return FromJson(content);
        }

        public static MojangLauncherAccounts? FromJson(string json)
        {
            return JsonConvert.DeserializeObject<MojangLauncherAccounts>(json);
        }
    }
}
